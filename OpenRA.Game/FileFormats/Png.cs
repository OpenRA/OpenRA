#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using System.Text;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.FileFormats
{
	public class Png
	{
		static readonly byte[] Signature = { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

		public int Width { get; }
		public int Height { get; }
		public Color[] Palette { get; }
		public byte[] Data { get; }
		public SpriteFrameType Type { get; }
		public Dictionary<string, string> EmbeddedData = new Dictionary<string, string>();

		public int PixelStride => Type == SpriteFrameType.Indexed8 ? 1 : Type == SpriteFrameType.Rgb24 ? 3 : 4;

		public Png(Stream s)
		{
			if (!Verify(s))
				throw new InvalidDataException("PNG Signature is bogus");

			s.Position += 8;
			var headerParsed = false;
			var data = new List<byte>();
			Type = SpriteFrameType.Rgba32;

			while (true)
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
							if (IsPaletted(bitDepth, colorType))
								Type = SpriteFrameType.Indexed8;
							else if (colorType == PngColorType.Color)
								Type = SpriteFrameType.Rgb24;

							Data = new byte[Width * Height * PixelStride];

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
									var pxStride = PixelStride;
									var rowStride = Width * pxStride;

									var prevLine = new byte[rowStride];
									for (var y = 0; y < Height; y++)
									{
										var filter = (PngFilter)ds.ReadByte();
										var line = ds.ReadBytes(rowStride);

										for (var i = 0; i < rowStride; i++)
											line[i] = i < pxStride
												? UnapplyFilter(filter, line[i], 0, prevLine[i], 0)
												: UnapplyFilter(filter, line[i], line[i - pxStride], prevLine[i], prevLine[i - pxStride]);

										Array.Copy(line, 0, Data, y * rowStride, rowStride);

										prevLine = line;
									}
								}
							}

							if (Type == SpriteFrameType.Indexed8 && Palette == null)
								throw new InvalidDataException("Non-Palette indexed PNG are not supported.");

							return;
						}
					}
				}
			}
		}

		public Png(byte[] data, SpriteFrameType type, int width, int height, Color[] palette = null,
			Dictionary<string, string> embeddedData = null)
		{
			var expectLength = width * height;
			if (palette == null)
				expectLength *= 4;

			if (data.Length != expectLength)
				throw new InvalidDataException("Input data does not match expected length");

			Type = type;
			Width = width;
			Height = height;

			switch (type)
			{
				case SpriteFrameType.Indexed8:
				case SpriteFrameType.Rgba32:
				case SpriteFrameType.Rgb24:
				{
					// Data is already in a compatible format
					Data = data;
					if (type == SpriteFrameType.Indexed8)
						Palette = palette;

					break;
				}

				case SpriteFrameType.Bgra32:
				case SpriteFrameType.Bgr24:
				{
					// Convert to big endian
					Data = new byte[data.Length];
					var stride = PixelStride;
					for (var i = 0; i < width * height; i++)
					{
						Data[stride * i] = data[stride * i + 2];
						Data[stride * i + 1] = data[stride * i + 1];
						Data[stride * i + 2] = data[stride * i + 0];

						if (type == SpriteFrameType.Bgra32)
							Data[stride * i + 3] = data[stride * i + 3];
					}

					break;
				}

				default:
					throw new InvalidDataException($"Unhandled SpriteFrameType {type}");
			}

			if (embeddedData != null)
				EmbeddedData = embeddedData;
		}

		public static bool Verify(Stream s)
		{
			var pos = s.Position;
			var isPng = Signature.Aggregate(true, (current, t) => current && s.ReadUInt8() == t);
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

		void WritePngChunk(Stream output, string type, Stream input)
		{
			input.Position = 0;

			var typeBytes = Encoding.ASCII.GetBytes(type);
			output.Write(IPAddress.HostToNetworkOrder((int)input.Length));
			output.WriteArray(typeBytes);

			var data = input.ReadAllBytes();
			output.WriteArray(data);

			var crc32 = new Crc32();
			crc32.Update(typeBytes);
			crc32.Update(data);
			output.Write(IPAddress.NetworkToHostOrder((int)crc32.Value));
		}

		public byte[] Save()
		{
			using (var output = new MemoryStream())
			{
				output.WriteArray(Signature);
				using (var header = new MemoryStream())
				{
					header.Write(IPAddress.HostToNetworkOrder(Width));
					header.Write(IPAddress.HostToNetworkOrder(Height));
					header.WriteByte(8); // Bit depth

					var colorType = Type == SpriteFrameType.Indexed8 ? PngColorType.Indexed | PngColorType.Color :
						Type == SpriteFrameType.Rgb24 ? PngColorType.Color : PngColorType.Color | PngColorType.Alpha;
					header.WriteByte((byte)colorType);

					header.WriteByte(0); // Compression
					header.WriteByte(0); // Filter
					header.WriteByte(0); // Interlacing

					WritePngChunk(output, "IHDR", header);
				}

				var alphaPalette = false;
				if (Palette != null)
				{
					using (var palette = new MemoryStream())
					{
						foreach (var c in Palette)
						{
							palette.WriteByte(c.R);
							palette.WriteByte(c.G);
							palette.WriteByte(c.B);
							alphaPalette |= c.A > 0;
						}

						WritePngChunk(output, "PLTE", palette);
					}
				}

				if (alphaPalette)
				{
					using (var alpha = new MemoryStream())
					{
						foreach (var c in Palette)
							alpha.WriteByte(c.A);

						WritePngChunk(output, "tRNS", alpha);
					}
				}

				using (var data = new MemoryStream())
				{
					using (var compressed = new DeflaterOutputStream(data))
					{
						var rowStride = Width * PixelStride;
						for (var y = 0; y < Height; y++)
						{
							// Write uncompressed scanlines for simplicity
							compressed.WriteByte(0);
							compressed.Write(Data, y * rowStride, rowStride);
						}

						compressed.Flush();
						compressed.Finish();

						WritePngChunk(output, "IDAT", data);
					}
				}

				foreach (var kv in EmbeddedData)
				{
					using (var text = new MemoryStream())
					{
						text.WriteArray(Encoding.ASCII.GetBytes(kv.Key + (char)0 + kv.Value));
						WritePngChunk(output, "tEXt", text);
					}
				}

				WritePngChunk(output, "IEND", new MemoryStream());
				return output.ToArray();
			}
		}

		public void Save(string path)
		{
			File.WriteAllBytes(path, Save());
		}
	}
}
