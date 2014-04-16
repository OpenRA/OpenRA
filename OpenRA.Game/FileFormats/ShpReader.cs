#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.FileFormats
{
	enum Format { Format20 = 0x20, Format40 = 0x40, Format80 = 0x80 }

	class ImageHeader : ISpriteFrame
	{
		public Size Size { get { return reader.Size; } }
		public Size FrameSize { get { return reader.Size; } }
		public float2 Offset { get { return float2.Zero; } }
		public byte[] Data { get; set; }

		public uint FileOffset;
		public Format Format;

		public uint RefOffset;
		public Format RefFormat;
		public ImageHeader RefImage;

		ShpReader reader;
		// Used by ShpWriter
		public ImageHeader() { }

		public ImageHeader(Stream stream, ShpReader reader)
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

	public class ShpReader : ISpriteSource
	{
		readonly List<ImageHeader> headers = new List<ImageHeader>();
		public IEnumerable<ISpriteFrame> Frames { get { return headers.Cast<ISpriteFrame>(); } }
		public bool CacheWhenLoadingTileset { get { return false; } }
		public readonly Size Size;

		int recurseDepth = 0;
		readonly int imageCount;

		public ShpReader(Stream stream)
		{
			imageCount = stream.ReadUInt16();
			stream.Position += 4;
			var width = stream.ReadUInt16();
			var height = stream.ReadUInt16();
			Size = new Size(width, height);

			stream.Position += 4;
			for (var i = 0; i < imageCount; i++)
				headers.Add(new ImageHeader(stream, this));

			// Skip eof and zero headers
			stream.Position += 16;

			var offsets = headers.ToDictionary(h => h.FileOffset, h => h);
			for (var i = 0; i < imageCount; i++)
			{
				var h = headers[i];
				if (h.Format == Format.Format20)
					h.RefImage = headers[i - 1];

				else if (h.Format == Format.Format40 && !offsets.TryGetValue(h.RefOffset, out h.RefImage))
					throw new InvalidDataException("Reference doesnt point to image data {0}->{1}".F(h.FileOffset, h.RefOffset));
			}

			foreach (var h in headers)
				Decompress(stream, h);
		}

		static byte[] ReadCompressedData(Stream stream, ImageHeader h)
		{
			stream.Position = h.FileOffset;

			// Actually, far too big. There's no length field with the correct length though :(
			var compressedLength = (int)(stream.Length - stream.Position);
			var compressedBytes = new byte[compressedLength];
			stream.Read(compressedBytes, 0, compressedLength);

			return compressedBytes;
		}

		void Decompress(Stream stream, ImageHeader h)
		{
			// No extra work is required for empty frames
			if (h.Size.Width == 0 || h.Size.Height == 0)
				return;

			if (recurseDepth > imageCount)
				throw new InvalidDataException("Format20/40 headers contain infinite loop");

			switch (h.Format)
			{
				case Format.Format20:
				case Format.Format40:
				{
					if (h.RefImage.Data == null)
					{
						++recurseDepth;
						Decompress(stream, h.RefImage);
						--recurseDepth;
					}

					h.Data = CopyImageData(h.RefImage.Data);
					Format40.DecodeInto(ReadCompressedData(stream, h), h.Data);
					break;
				}

				case Format.Format80:
				{
					var imageBytes = new byte[Size.Width * Size.Height];
					Format80.DecodeInto(ReadCompressedData(stream, h), imageBytes);
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
			for (var i = 0; i < Size.Width * Size.Height; i++)
				imageData[i] = baseImage[i];

			return imageData;
		}

		public static ShpReader Load(string filename)
		{
			using (var s = File.OpenRead(filename))
				return new ShpReader(s);
		}

		public static void Write(Stream s, Size size, IEnumerable<byte[]> frames)
		{
			var compressedFrames = frames.Select(f => Format80.Encode(f)).ToArray();

			// note: end-of-file and all-zeroes headers
			var dataOffset = 14 + (compressedFrames.Length + 2) * 8;

			using (var bw = new BinaryWriter(s))
			{
				bw.Write((ushort)compressedFrames.Length);
				bw.Write((ushort)0);
				bw.Write((ushort)0);
				bw.Write((ushort)size.Width);
				bw.Write((ushort)size.Height);
				bw.Write((uint)0);

				foreach (var f in compressedFrames)
				{
					var ih = new ImageHeader { Format = Format.Format80, FileOffset = (uint)dataOffset };
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
