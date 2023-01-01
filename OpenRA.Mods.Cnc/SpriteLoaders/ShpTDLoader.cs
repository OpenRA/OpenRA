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
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.SpriteLoaders
{
	public class ShpTDLoader : ISpriteLoader
	{
		static bool IsShpTD(Stream s)
		{
			var start = s.Position;

			// First word is the image count
			var imageCount = s.ReadUInt16();
			if (imageCount == 0)
			{
				s.Position = start;
				return false;
			}

			// Last offset should point to the end of file
			var finalOffset = start + 14 + 8 * imageCount;
			if (finalOffset > s.Length)
			{
				s.Position = start;
				return false;
			}

			s.Position = finalOffset;
			var eof = s.ReadUInt32();
			if (eof != s.Length)
			{
				s.Position = start;
				return false;
			}

			// Check the format flag on the first frame
			s.Position = start + 17;
			var b = s.ReadUInt8();

			s.Position = start;
			return b == 0x20 || b == 0x40 || b == 0x80;
		}

		public bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames, out TypeDictionary metadata)
		{
			metadata = null;
			if (!IsShpTD(s))
			{
				frames = null;
				return false;
			}

			frames = new ShpTDSprite(s).Frames.ToArray();
			return true;
		}
	}

	public class ShpTDSprite
	{
		enum Format { XORPrev = 0x20, XORLCW = 0x40, LCW = 0x80 }

		class ImageHeader : ISpriteFrame
		{
			public SpriteFrameType Type => SpriteFrameType.Indexed8;
			public Size Size => reader.Size;
			public Size FrameSize => reader.Size;
			public float2 Offset => float2.Zero;
			public byte[] Data { get; set; }
			public bool DisableExportPadding => false;

			public uint FileOffset;
			public Format Format;

			public readonly uint RefOffset;
			public readonly Format RefFormat;
			public ImageHeader RefImage;

			readonly ShpTDSprite reader;

			// Used by ShpWriter
			public ImageHeader() { }

			public ImageHeader(Stream stream, ShpTDSprite reader)
			{
				this.reader = reader;
				var data = stream.ReadUInt32();
				FileOffset = data & 0xffffff;
				Format = (Format)(data >> 24);

				RefOffset = stream.ReadUInt16();
				RefFormat = (Format)stream.ReadUInt16();
			}

			public void WriteTo(BinaryWriter writer)
			{
				writer.Write(FileOffset | ((uint)Format << 24));
				writer.Write((ushort)RefOffset);
				writer.Write((ushort)RefFormat);
			}
		}

		public IReadOnlyList<ISpriteFrame> Frames { get; }
		public readonly Size Size;

		int recurseDepth = 0;
		readonly int imageCount;

		readonly long shpBytesFileOffset;
		readonly byte[] shpBytes;

		public ShpTDSprite(Stream stream)
		{
			imageCount = stream.ReadUInt16();
			stream.Position += 4;
			var width = stream.ReadUInt16();
			var height = stream.ReadUInt16();
			Size = new Size(width, height);

			stream.Position += 4;
			var headers = new ImageHeader[imageCount];
			for (var i = 0; i < headers.Length; i++)
				headers[i] = new ImageHeader(stream, this);

			// Skip eof and zero headers
			stream.Position += 16;

			var offsets = headers.ToDictionary(h => h.FileOffset, h => h);
			for (var i = 0; i < imageCount; i++)
			{
				var h = headers[i];
				if (h.Format == Format.XORPrev)
					h.RefImage = headers[i - 1];
				else if (h.Format == Format.XORLCW && !offsets.TryGetValue(h.RefOffset, out h.RefImage))
					throw new InvalidDataException($"Reference doesn't point to image data {h.FileOffset}->{h.RefOffset}");
			}

			shpBytesFileOffset = stream.Position;
			shpBytes = stream.ReadBytes((int)(stream.Length - stream.Position));

			foreach (var h in headers)
				Decompress(h);

			Frames = headers;
		}

		void Decompress(ImageHeader h)
		{
			// No extra work is required for empty frames
			if (h.Size.Width == 0 || h.Size.Height == 0)
				return;

			if (recurseDepth > imageCount)
				throw new InvalidDataException("Format20/40 headers contain infinite loop");

			switch (h.Format)
			{
				case Format.XORPrev:
				case Format.XORLCW:
					{
						if (h.RefImage.Data == null)
						{
							++recurseDepth;
							Decompress(h.RefImage);
							--recurseDepth;
						}

						h.Data = CopyImageData(h.RefImage.Data);
						XORDeltaCompression.DecodeInto(shpBytes, h.Data, (int)(h.FileOffset - shpBytesFileOffset));
						break;
					}

				case Format.LCW:
					{
						var imageBytes = new byte[Size.Width * Size.Height];
						LCWCompression.DecodeInto(shpBytes, imageBytes, (int)(h.FileOffset - shpBytesFileOffset));
						h.Data = imageBytes;
						break;
					}

				default:
					throw new InvalidDataException();
			}
		}

		byte[] CopyImageData(byte[] baseImage)
		{
			var imageData = new byte[Size.Width * Size.Height];
			Array.Copy(baseImage, imageData, imageData.Length);
			return imageData;
		}

		public static void Write(Stream s, Size size, IEnumerable<byte[]> frames)
		{
			var compressedFrames = frames.Select(f => LCWCompression.Encode(f)).ToList();

			// note: end-of-file and all-zeroes headers
			var dataOffset = 14 + (compressedFrames.Count + 2) * 8;

			using (var bw = new BinaryWriter(s))
			{
				bw.Write((ushort)compressedFrames.Count);
				bw.Write((ushort)0);
				bw.Write((ushort)0);
				bw.Write((ushort)size.Width);
				bw.Write((ushort)size.Height);
				bw.Write(0U);

				foreach (var f in compressedFrames)
				{
					var ih = new ImageHeader { Format = Format.LCW, FileOffset = (uint)dataOffset };
					dataOffset += f.Length;

					ih.WriteTo(bw);
				}

				var eof = new ImageHeader { FileOffset = (uint)dataOffset };
				eof.WriteTo(bw);

				var allZeroes = new ImageHeader { };
				allZeroes.WriteTo(bw);

				foreach (var f in compressedFrames)
					bw.Write(f);
			}
		}
	}
}
