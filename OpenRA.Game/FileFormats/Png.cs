#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using ICSharpCode.SharpZipLib.Checksums;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using OpenRA.Primitives;

namespace OpenRA.FileFormats
{
	public class Png
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public Color[] Palette { get; set; }
		public byte[] Data { get; set; }
		public Dictionary<string, string> EmbeddedData = new Dictionary<string, string>();

		public Png(Stream s)
		{
			if (!Verify(s))
				throw new InvalidDataException("PNG Signature is bogus");

			s.Position += 8;
			var headerParsed = false;
			var isPaletted = false;
			var is24Bit = false;
			var data = new List<byte>();

			for (;;)
			{
				var length = IPAddress.NetworkToHostOrder(s.ReadInt32());
				var type = Encoding.UTF8.GetString(s.ReadBytes(4));
				var content = s.ReadBytes(length);
				/*var crc = */s.ReadInt32();

				if (!headerParsed && type != "IHDR")
					throw new InvalidDataException("Invalid PNG file - header does not appear first.");

				using (var ms = new MemoryStream(content))
				{
					switch (type)
					{
						case "IHDR":
						{
							if (headerParsed)
								throw new InvalidDataException("Invalid PNG file - duplicate header.");
							Width = IPAddress.NetworkToHostOrder(ms.ReadInt32());
							Height = IPAddress.NetworkToHostOrder(ms.ReadInt32());

							var bitDepth = ms.ReadUInt8();
							var colorType = (PngColorType)ms.ReadByte();
							isPaletted = IsPaletted(bitDepth, colorType);
							is24Bit = colorType == PngColorType.Color;

							var dataLength = Width * Height;
							if (!isPaletted)
								dataLength *= 4;

							Data = new byte[dataLength];

							var compression = ms.ReadByte();
							/*var filter = */ms.ReadByte();
							var interlace = ms.ReadByte();

							if (compression != 0)
								throw new InvalidDataException("Compression method not supported");

							if (interlace != 0)
								throw new InvalidDataException("Interlacing not supported");

							headerParsed = true;

							break;
						}

						case "PLTE":
						{
							Palette = new Color[256];
							for (var i = 0; i < length / 3; i++)
							{
								var r = ms.ReadByte(); var g = ms.ReadByte(); var b = ms.ReadByte();
								Palette[i] = Color.FromArgb(r, g, b);
							}

							break;
						}

						case "tRNS":
						{
							if (Palette == null)
								throw new InvalidDataException("Non-Palette indexed PNG are not supported.");

							for (var i = 0; i < length; i++)
								Palette[i] = Color.FromArgb(ms.ReadByte(), Palette[i]);

							break;
						}

						case "IDAT":
						{
							data.AddRange(content);

							break;
						}

						case "tEXt":
						{
							var key = ms.ReadASCIIZ();
							EmbeddedData.Add(key, ms.ReadASCII(length - key.Length - 1));

							break;
						}

						case "IEND":
						{
							using (var ns = new MemoryStream(data.ToArray()))
							{
								using (var ds = new InflaterInputStream(ns))
								{
									var pxStride = isPaletted ? 1 : is24Bit ? 3 : 4;
									var srcStride = Width * pxStride;
									var destStride = Width * (isPaletted ? 1 : 4);

									var prevLine = new byte[srcStride];
									for (var y = 0; y < Height; y++)
									{
										var filter = (PngFilter)ds.ReadByte();
										var line = ds.ReadBytes(srcStride);

										for (var i = 0; i < srcStride; i++)
											line[i] = i < pxStride
												? UnapplyFilter(filter, line[i], 0, prevLine[i], 0)
												: UnapplyFilter(filter, line[i], line[i - pxStride], prevLine[i], prevLine[i - pxStride]);

										if (is24Bit)
										{
											// Fold alpha channel into RGB data
											for (var i = 0; i < line.Length / 3; i++)
											{
												Array.Copy(line, 3 * i, Data, y * destStride + 4 * i, 3);
												Data[y * destStride + 4 * i + 3] = 255;
											}
										}
										else
											Array.Copy(line, 0, Data, y * destStride, line.Length);

										prevLine = line;
									}
								}
							}

							if (isPaletted && Palette == null)
								throw new InvalidDataException("Non-Palette indexed PNG are not supported.");

							return;
						}
					}
				}
			}
		}

		public Png(byte[] data, int width, int height, Color[] palette = null,
			Dictionary<string, string> embeddedData = null)
		{
			var expectLength = width * height;
			if (palette == null)
				expectLength *= 4;

			if (data.Length != expectLength)
				throw new InvalidDataException("Input data does not match expected length");

			Width = width;
			Height = height;

			Palette = palette;
			Data = data;

			if (embeddedData != null)
				EmbeddedData = embeddedData;
		}

		public static bool Verify(Stream s)
		{
			var pos = s.Position;
			var signature = new[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
			var isPng = signature.Aggregate(true, (current, t) => current && s.ReadUInt8() == t);
			s.Position = pos;
			return isPng;
		}

		static byte UnapplyFilter(PngFilter f, byte x, byte a, byte b, byte c)
		{
			switch (f)
			{
				case PngFilter.None: return x;
				case PngFilter.Sub: return (byte)(x + a);
				case PngFilter.Up: return (byte)(x + b);
				case PngFilter.Average: return (byte)(x + (a + b) / 2);
				case PngFilter.Paeth: return (byte)(x + Paeth(a, b, c));
				default:
					throw new InvalidOperationException("Unsupported Filter");
			}
		}

		static byte Paeth(byte a, byte b, byte c)
		{
			var p = a + b - c;
			var pa = Math.Abs(p - a);
			var pb = Math.Abs(p - b);
			var pc = Math.Abs(p - c);

			return (pa <= pb && pa <= pc) ? a :
				(pb <= pc) ? b : c;
		}

		[Flags]
		enum PngColorType { Indexed = 1, Color = 2, Alpha = 4 }
		enum PngFilter { None, Sub, Up, Average, Paeth }

		static bool IsPaletted(byte bitDepth, PngColorType colorType)
		{
			if (bitDepth == 8 && colorType == (PngColorType.Indexed | PngColorType.Color))
				return true;

			if (bitDepth == 8 && colorType == (PngColorType.Color | PngColorType.Alpha))
				return false;

			if (bitDepth == 8 && colorType == PngColorType.Color)
				return false;

			throw new InvalidDataException("Unknown pixel format");
		}

		public byte[] Save()
		{
			var pixelFormat = Palette != null ? System.Drawing.Imaging.PixelFormat.Format8bppIndexed : System.Drawing.Imaging.PixelFormat.Format32bppArgb;

			// Save to a memory stream that we can then parse to add the embedded data
			using (var bitmapStream = new MemoryStream())
			{
				using (var bitmap = new System.Drawing.Bitmap(Width, Height, pixelFormat))
				{
					if (Palette != null)
					{
						// Setting bitmap.Palette.Entries directly doesn't work
						var bPal = bitmap.Palette;
						for (var i = 0; i < 256; i++)
							bPal.Entries[i] = System.Drawing.Color.FromArgb(Palette[i].ToArgb());

						bitmap.Palette = bPal;
						var bd = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, Width, Height), System.Drawing.Imaging.ImageLockMode.WriteOnly,
							pixelFormat);
						for (var i = 0; i < Height; i++)
							Marshal.Copy(Data, i * Width, IntPtr.Add(bd.Scan0, i * bd.Stride), Width);

						bitmap.UnlockBits(bd);
					}
					else
					{
						unsafe
						{
							var bd = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, Width, Height), System.Drawing.Imaging.ImageLockMode.WriteOnly, pixelFormat);
							var colors = (int*)bd.Scan0;
							for (var y = 0; y < Height; y++)
							{
								for (var x = 0; x < Width; x++)
								{
									var i = y * Width + x;

									// Convert RGBA to ARGB
									colors[i] = Color.FromArgb(Data[4 * i + 3], Data[4 * i + 0], Data[4 * i + 1],
										Data[4 * i + 2]).ToArgb();
								}
							}

							bitmap.UnlockBits(bd);
						}
					}

					bitmap.Save(bitmapStream, System.Drawing.Imaging.ImageFormat.Png);
				}

				if (!EmbeddedData.Any())
					return bitmapStream.ToArray();

				// Add embedded metadata to the end of the image
				bitmapStream.Position = 0;
				var outputStream = new MemoryStream();
				using (var bw = new BinaryWriter(outputStream))
				{
					bw.Write(bitmapStream.ReadBytes(8));
					var crc32 = new Crc32();

					for (;;)
					{
						var length = IPAddress.NetworkToHostOrder(bitmapStream.ReadInt32());
						var type = Encoding.UTF8.GetString(bitmapStream.ReadBytes(4));
						var content = bitmapStream.ReadBytes(length);
						var crc = bitmapStream.ReadUInt32();

						switch (type)
						{
							case "tEXt":
								break;

							case "IEND":
								bitmapStream.Close();

								foreach (var kv in EmbeddedData)
								{
									bw.Write(IPAddress.NetworkToHostOrder(kv.Key.Length + 1 + kv.Value.Length));
									bw.Write("tEXt".ToCharArray());
									bw.Write(kv.Key.ToCharArray());
									bw.Write((byte)0x00);
									bw.Write(kv.Value.ToCharArray());
									crc32.Reset();
									crc32.Update(Encoding.ASCII.GetBytes("tEXt"));
									crc32.Update(Encoding.ASCII.GetBytes(kv.Key + (char)0x00 + kv.Value));
									bw.Write((uint)IPAddress.NetworkToHostOrder((int)crc32.Value));
								}

								bw.Write(0);
								bw.Write(type.ToCharArray());
								bw.Write(crc);

								return outputStream.ToArray();

							default:
								bw.Write(IPAddress.NetworkToHostOrder(length));
								bw.Write(type.ToCharArray());
								bw.Write(content);
								bw.Write(crc);
								break;
						}
					}
				}
			}
		}

		public void Save(string path)
		{
			File.WriteAllBytes(path, Save());
		}
	}
}
