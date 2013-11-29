#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.FileFormats
{
	class ImageHeader : ISpriteFrame
	{
		public Size Size { get; private set; }
		public Size FrameSize { get { return Size; } }
		public float2 Offset { get { return float2.Zero; } }
		public byte[] Data { get; set; }

		public uint FileOffset;
		public Format Format;

		public uint RefOffset;
		public Format RefFormat;
		public ImageHeader RefImage;

		// Used by ShpWriter
		public ImageHeader() { }

		public ImageHeader(BinaryReader reader, Size size)
		{
			var data = reader.ReadUInt32();
			Size = size;
			FileOffset = data & 0xffffff;
			Format = (Format)(data >> 24);

			RefOffset = reader.ReadUInt16();
			RefFormat = (Format)reader.ReadUInt16();
		}

		public static readonly int SizeOnDisk = 8;

		public void WriteTo(BinaryWriter writer)
		{
			writer.Write(FileOffset | ((uint)Format << 24));
			writer.Write((ushort)RefOffset);
			writer.Write((ushort)RefFormat);
		}
	}

	public enum Format { Format20 = 0x20, Format40 = 0x40, Format80 = 0x80 }

	public class ShpReader : ISpriteSource
	{
		public readonly int ImageCount;
		public readonly ushort Width;
		public readonly ushort Height;

		public Size Size { get { return new Size(Width, Height); } }

		readonly List<ImageHeader> headers = new List<ImageHeader>();
		public IEnumerable<ISpriteFrame> Frames { get { return headers.Cast<ISpriteFrame>(); } }

		int recurseDepth = 0;

		public ShpReader(Stream stream)
		{
			using (var reader = new BinaryReader(stream))
			{
				ImageCount = reader.ReadUInt16();
				reader.ReadUInt16();
				reader.ReadUInt16();
				Width = reader.ReadUInt16();
				Height = reader.ReadUInt16();
				reader.ReadUInt32();

				var size = new Size(Width, Height);
				for (int i = 0 ; i < ImageCount ; i++)
					headers.Add(new ImageHeader(reader, size));

				new ImageHeader(reader, size); // end-of-file header
				new ImageHeader(reader, size); // all-zeroes header

				var offsets = headers.ToDictionary(h => h.FileOffset, h =>h);

				for (int i = 0 ; i < ImageCount ; i++)
				{
					var h = headers[ i ];
					if (h.Format == Format.Format20)
						h.RefImage = headers[i - 1];

					else if (h.Format == Format.Format40)
						if (!offsets.TryGetValue(h.RefOffset, out h.RefImage))
							throw new InvalidDataException("Reference doesnt point to image data {0}->{1}".F(h.FileOffset, h.RefOffset));
				}

				foreach (ImageHeader h in headers)
					Decompress(stream, h);
			}
		}

		void Decompress(Stream stream, ImageHeader h)
		{
			if (recurseDepth > ImageCount)
				throw new InvalidDataException("Format20/40 headers contain infinite loop");

			switch(h.Format)
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
						var imageBytes = new byte[Width * Height];
						Format80.DecodeInto(ReadCompressedData(stream, h), imageBytes);
						h.Data = imageBytes;
						break;
					}
				default:
					throw new InvalidDataException();
			}
		}

		static byte[] ReadCompressedData(Stream stream, ImageHeader h)
		{
			stream.Position = h.FileOffset;
			// TODO: Actually, far too big. There's no length field with the correct length though :(
			var compressedLength = (int)(stream.Length - stream.Position);

			var compressedBytes = new byte[ compressedLength ];
			stream.Read( compressedBytes, 0, compressedLength );

			return compressedBytes;
		}

		byte[] CopyImageData(byte[] baseImage)
		{
			var imageData = new byte[Width * Height];
			for (int i = 0 ; i < Width * Height ; i++)
				imageData[i] = baseImage[i];

			return imageData;
		}

		public static ShpReader Load(string filename)
		{
			using (var s = File.OpenRead(filename))
				return new ShpReader(s);
		}

		public static void Write(Stream s, int width, int height, IEnumerable<byte[]> frames)
		{
			var compressedFrames = frames.Select(f => Format80.Encode(f)).ToArray();

			// note: end-of-file and all-zeroes headers
			var dataOffset = 14 + (compressedFrames.Length + 2) * ImageHeader.SizeOnDisk;

			using (var bw = new BinaryWriter(s))
			{
				bw.Write((ushort)compressedFrames.Length);
				bw.Write((ushort)0);	// unused
				bw.Write((ushort)0);	// unused
				bw.Write((ushort)width);
				bw.Write((ushort)height);
				bw.Write((uint)0);		// unused

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
