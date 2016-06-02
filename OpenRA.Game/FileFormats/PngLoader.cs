#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace OpenRA.FileFormats
{
	public static class PngLoader
	{
		public static Bitmap Load(string filename)
		{
			using (var s = File.OpenRead(filename))
				return Load(s);
		}

		public static Bitmap Load(Stream s)
		{
			using (var br = new BinaryReader(s))
			{
				var signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
				foreach (var b in signature)
					if (br.ReadByte() != b)
						throw new InvalidDataException("PNG Signature is bogus");

				Bitmap bitmap = null;
				Color[] palette = null;
				var data = new List<byte>();

				try
				{
					for (;;)
					{
						var length = IPAddress.NetworkToHostOrder(br.ReadInt32());
						var type = Encoding.UTF8.GetString(br.ReadBytes(4));
						var content = br.ReadBytes(length);
						/*var crc = */br.ReadInt32();

						if (bitmap == null && type != "IHDR")
							throw new InvalidDataException("Invalid PNG file - header does not appear first.");

						using (var ms = new MemoryStream(content))
						using (var cr = new BinaryReader(ms))
							switch (type)
							{
								case "IHDR":
									{
										if (bitmap != null)
											throw new InvalidDataException("Invalid PNG file - duplicate header.");

										var width = IPAddress.NetworkToHostOrder(cr.ReadInt32());
										var height = IPAddress.NetworkToHostOrder(cr.ReadInt32());
										var bitDepth = cr.ReadByte();
										var colorType = (PngColorType)cr.ReadByte();
										var compression = cr.ReadByte();
										/*var filter = */cr.ReadByte();
										var interlace = cr.ReadByte();

										if (compression != 0) throw new InvalidDataException("Compression method not supported");
										if (interlace != 0) throw new InvalidDataException("Interlacing not supported");

										bitmap = new Bitmap(width, height, MakePixelFormat(bitDepth, colorType));
									}

									break;

								case "PLTE":
									{
										palette = new Color[256];
										for (var i = 0; i < 256; i++)
										{
											var r = cr.ReadByte(); var g = cr.ReadByte(); var b = cr.ReadByte();
											palette[i] = Color.FromArgb(r, g, b);
										}
									}

									break;

								case "tRNS":
									{
										if (palette == null)
											throw new InvalidDataException("Non-Palette indexed PNG are not supported.");

										for (var i = 0; i < length; i++)
											palette[i] = Color.FromArgb(cr.ReadByte(), palette[i]);
									}

									break;

								case "IDAT":
									{
										data.AddRange(content);
									}

									break;

								case "IEND":
									{
										var bits = bitmap.LockBits(bitmap.Bounds(),
											ImageLockMode.WriteOnly, PixelFormat.Format8bppIndexed);

										using (var ns = new MemoryStream(data.ToArray()))
										{
											// 'zlib' flags bytes; confuses the DeflateStream.
											/*var flags = (byte)*/ns.ReadByte();
											/*var moreFlags = (byte)*/ns.ReadByte();

											using (var ds = new DeflateStream(ns, CompressionMode.Decompress))
											using (var dr = new BinaryReader(ds))
											{
												var prevLine = new byte[bitmap.Width];  // all zero
												for (var y = 0; y < bitmap.Height; y++)
												{
													var filter = (PngFilter)dr.ReadByte();
													var line = dr.ReadBytes(bitmap.Width);

													for (var i = 0; i < bitmap.Width; i++)
														line[i] = i > 0
															? UnapplyFilter(filter, line[i], line[i - 1], prevLine[i], prevLine[i - 1])
															: UnapplyFilter(filter, line[i], 0, prevLine[i], 0);

													Marshal.Copy(line, 0, new IntPtr(bits.Scan0.ToInt64() + y * bits.Stride), line.Length);
													prevLine = line;
												}
											}
										}

										bitmap.UnlockBits(bits);

										if (palette == null)
											throw new InvalidDataException("Non-Palette indexed PNG are not supported.");

										using (var temp = new Bitmap(1, 1, PixelFormat.Format8bppIndexed))
										{
											var cp = temp.Palette;
											for (var i = 0; i < 256; i++)
												cp.Entries[i] = palette[i];     // finalize the palette.
											bitmap.Palette = cp;
											return bitmap;
										}
									}
							}
					}
				}
				catch
				{
					if (bitmap != null)
						bitmap.Dispose();
					throw;
				}
			}
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

		static PixelFormat MakePixelFormat(byte bitDepth, PngColorType colorType)
		{
			if (bitDepth == 8 && colorType == (PngColorType.Indexed | PngColorType.Color))
				return PixelFormat.Format8bppIndexed;

			throw new InvalidDataException("Unknown pixel format");
		}
	}
}
