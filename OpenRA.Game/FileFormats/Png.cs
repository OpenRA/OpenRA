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
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using OpenRA.Graphics;
using OpenRA.Primitives;

namespace OpenRA.FileFormats
{
	/// <summary>
	/// Used to connect several IDAT chunks into a continuous stream.
	/// </summary>
	sealed class PngIdatStream : Stream
	{
		readonly Stream baseStream;
		int remainingInChunk;
		bool eof;

		public PngIdatStream(Stream baseStream, int initialLen)
		{
			this.baseStream = baseStream;
			remainingInChunk = initialLen;
		}

		public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

		public override int Read(Span<byte> buffer)
		{
			if (eof || buffer.Length == 0)
				return 0;

			var totalRead = 0;
			Span<byte> header = stackalloc byte[8];
			while (buffer.Length > 0)
			{
				if (remainingInChunk == 0)
				{
					// Skip CRC and read next chunk header.
					var r = baseStream.Seek(4, SeekOrigin.Current) + baseStream.Read(header);
					if (r < 12)
						throw new EndOfStreamException("Invalid PNG file - no end chunk found.");

					// The PNG spec states that IDAT chunks must be chained together.
					if (BinaryPrimitives.ReadUInt32BigEndian(header[4..]) == Png.ChunkIDAT)
						remainingInChunk = BinaryPrimitives.ReadInt32BigEndian(header[..4]);
					else
					{
						// This is not an IDAT chunk. Discontinue reading.
						baseStream.Seek(-8, SeekOrigin.Current);
						eof = true;
						return totalRead;
					}
				}

				var toRead = Math.Min(buffer.Length, remainingInChunk);
				var read = baseStream.Read(buffer[..toRead]);
				if (read == 0)
					throw new EndOfStreamException("Unexpected end of stream in IDAT chunk.");

				remainingInChunk -= read;
				totalRead += read;
				buffer = buffer[read..];
			}

			return totalRead;
		}

		public override bool CanRead => true;
		public override bool CanSeek => false;
		public override bool CanWrite => false;
		public override long Length => throw new NotSupportedException();
		public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
		public override void Flush() { }
		public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
		public override void SetLength(long value) => throw new NotSupportedException();
		public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
	}

	public class Png
	{
		public const uint ChunkIHDR = 0x49484452;
		public const uint ChunkPLTE = 0x504C5445;
		public const uint ChunkIDAT = 0x49444154;
		public const uint ChunkIEND = 0x49454E44;
		public const uint ChunkTRNS = 0x74524E53;
		public const uint ChunkTEXT = 0x74455874;

		static readonly byte[] Signature = [0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a];

		public int Width { get; }
		public int Height { get; }
		public Color[] Palette { get; }
		public byte[] Data { get; }
		public SpriteFrameType Type { get; }
		public Dictionary<string, string> EmbeddedData = [];

		public int PixelStride => Type == SpriteFrameType.Indexed8 ? 1 : Type == SpriteFrameType.Rgb24 ? 3 : 4;

		public Png(Stream s)
		{
			if (!Verify(s))
				throw new InvalidDataException("PNG Signature is bogus");

			s.Position += 8;
			var headerParsed = false;
			var dataParsed = false;
			byte bitDepth = 8;
			Type = SpriteFrameType.Rgba32;

			// Use a reusable 8-byte buffer for Length + Type.
			Span<byte> chunkHeader = stackalloc byte[8];

			while (true)
			{
				if (s.Read(chunkHeader) < 8)
					throw new EndOfStreamException("Invalid PNG file - no end chunk found.");

				var length = BinaryPrimitives.ReadInt32BigEndian(chunkHeader[..4]);
				var type = BinaryPrimitives.ReadUInt32BigEndian(chunkHeader[4..]);

				if (!headerParsed && type != ChunkIHDR)
					throw new InvalidDataException("Invalid PNG file - header does not appear first.");

				switch (type)
				{
					case ChunkIHDR:
					{
						if (headerParsed)
							throw new InvalidDataException("Invalid PNG file - duplicate header.");

#pragma warning disable CA2014 // Do not use stackalloc in loops
						Span<byte> buffer = stackalloc byte[13];
#pragma warning restore CA2014 // Do not use stackalloc in loops
						s.ReadBytes(buffer);

						Width = BinaryPrimitives.ReadInt32BigEndian(buffer[..4]);
						Height = BinaryPrimitives.ReadInt32BigEndian(buffer[4..8]);
						bitDepth = buffer[8];
						var colorType = (PngColorType)buffer[9];

						if (IsPaletted(bitDepth, colorType))
							Type = SpriteFrameType.Indexed8;
						else if (colorType == PngColorType.Color)
							Type = SpriteFrameType.Rgb24;

						var compression = buffer[10];

						// filter = buffer[11]
						var interlace = buffer[12];

						if (compression != 0)
							throw new InvalidDataException("Compression method not supported");

						if (interlace != 0)
							throw new InvalidDataException("Interlacing not supported");

						Data = new byte[Width * Height * PixelStride];

						// Skip CRC.
						s.Seek(4, SeekOrigin.Current);
						headerParsed = true;
						break;
					}

					case ChunkPLTE:
					{
						if (length % 3 != 0)
							throw new InvalidDataException("Invalid PLTE chunk length.");

						var count = length / 3;
						Palette = new Color[count];

						var buffer = ArrayPool<byte>.Shared.Rent(length);
						try
						{
							s.ReadBytes(buffer.AsSpan(0, length));
							for (var i = 0; i < count; i++)
							{
								var offset = i * 3;
								Palette[i] = Color.FromArgb(buffer[offset], buffer[offset + 1], buffer[offset + 2]);
							}
						}
						finally
						{
							ArrayPool<byte>.Shared.Return(buffer);
						}

						// Skip CRC.
						s.Seek(4, SeekOrigin.Current);
						break;
					}

					case ChunkTRNS:
					{
						if (Palette == null)
							throw new InvalidDataException("Non-Palette indexed PNG are not supported.");

						var buffer = ArrayPool<byte>.Shared.Rent(length);
						try
						{
							s.ReadBytes(buffer.AsSpan(0, length));
							for (var i = 0; i < length && i < Palette.Length; i++)
								Palette[i] = Color.FromArgb(buffer[i], Palette[i]);
						}
						finally
						{
							ArrayPool<byte>.Shared.Return(buffer);
						}

						// Skip CRC.
						s.Seek(4, SeekOrigin.Current);
						break;
					}

					case ChunkIDAT:
						if (dataParsed)
							throw new InvalidDataException("Invalid PNG file - discontinuous IDAT chunks.");

						dataParsed = true;

						ProcessIDAT(s, length, bitDepth);
						break;

					case ChunkTEXT:
					{
						var buffer = ArrayPool<byte>.Shared.Rent(length);
						try
						{
							var span = buffer.AsSpan(0, length);
							s.ReadBytes(span);

							// Find Null Terminator for ASCIIZ (Keyword).
							var nullIndex = span.IndexOf((byte)0);
							if (nullIndex == -1)
								return;

							var key = Encoding.ASCII.GetString(span[..nullIndex]);
							var value = Encoding.ASCII.GetString(span[(nullIndex + 1)..]);

							EmbeddedData[key] = value;
						}
						finally
						{
							ArrayPool<byte>.Shared.Return(buffer);
						}

						// Skip CRC.
						s.Seek(4, SeekOrigin.Current);
						break;
					}

					case ChunkIEND:
					{
						if (Type == SpriteFrameType.Indexed8 && Palette == null)
							throw new InvalidDataException("Non-Palette indexed PNG are not supported.");

						// Skip CRC.
						s.Seek(4, SeekOrigin.Current);
						return;
					}

					default:
					{
						// Skip unknown chunk + CRC.
						s.Seek(length + 4, SeekOrigin.Current);
						break;
					}
				}
			}
		}

		void ProcessIDAT(Stream s, int firstChunkLength, byte bitDepth)
		{
			var pxStride = PixelStride;
			var rowStride = Width * pxStride;
			var pixelsPerByte = 8 / bitDepth;
			var sourceRowStride = Exts.IntegerDivisionRoundingAwayFromZero(rowStride, pixelsPerByte);

			// Custom stream that stitches IDAT chunks together without copying.
			using var idatStream = new PngIdatStream(s, firstChunkLength);
			using var ds = new ZLibStream(idatStream, CompressionMode.Decompress);

			// Rent buffer from pool to avoid allocation churn.
			var prevLineBuffer = ArrayPool<byte>.Shared.Rent(rowStride);
			var prevLine = prevLineBuffer.AsSpan(0, rowStride);
			prevLine.Clear();

			try
			{
				for (var y = 0; y < Height; y++)
				{
					var filterByte = ds.ReadByte();
					if (filterByte == -1)
						break;

					var filter = (PngFilter)filterByte;
					var line = Data.AsSpan(y * rowStride, rowStride);

					ds.ReadBytes(line[..sourceRowStride]);

					// If the source has a bit depth of 1, 2 or 4 it packs multiple pixels per byte.
					// Unpack to bit depth of 8, yielding 1 pixel per byte.
					// This makes life easier for consumers of palleted data.
					if (bitDepth < 8)
					{
						var mask = (1 << bitDepth) - 1;
						for (var i = sourceRowStride - 1; i >= 0; i--)
						{
							var packed = line[i];
							for (var j = 0; j < pixelsPerByte; j++)
							{
								var dest = i * pixelsPerByte + j;
								if (dest < line.Length) // Guard against last byte being only partially packed
									line[dest] = (byte)((packed >> (8 - (j + 1) * bitDepth)) & mask);
							}
						}
					}

					switch (filter)
					{
						case PngFilter.None:
							break;
						case PngFilter.Sub:
							for (var i = pxStride; i < line.Length; i++)
								line[i] += line[i - pxStride];
							break;
						case PngFilter.Up:
							for (var i = 0; i < line.Length; i++)
								line[i] += prevLine[i];
							break;
						case PngFilter.Average:
							for (var i = 0; i < pxStride; i++)
								line[i] += Average(0, prevLine[i]);
							for (var i = pxStride; i < line.Length; i++)
								line[i] += Average(line[i - pxStride], prevLine[i]);
							break;
						case PngFilter.Paeth:
							for (var i = 0; i < pxStride; i++)
								line[i] += Paeth(0, prevLine[i], 0);
							for (var i = pxStride; i < line.Length; i++)
								line[i] += Paeth(line[i - pxStride], prevLine[i], prevLine[i - pxStride]);
							break;
						default:
							throw new InvalidOperationException("Unsupported Filter");
					}

					prevLine = line;
				}

				// Drain remaining Zlib footer bytes if necessary.
				Span<byte> drain = stackalloc byte[16];
				while (ds.Read(drain) > 0) { }
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(prevLineBuffer);
			}

			static byte Average(byte a, byte b) => (byte)((a + b) / 2);

			static byte Paeth(byte a, byte b, byte c)
			{
				var p = a + b - c;
				var pa = Math.Abs(p - a);
				var pb = Math.Abs(p - b);
				var pc = Math.Abs(p - c);

				return (pa <= pb && pa <= pc) ? a :
					(pb <= pc) ? b : c;
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
					var elements = width * height * stride;
					var src = data.AsSpan();
					var dst = Data.AsSpan();
					if (type == SpriteFrameType.Bgra32)
					{
						for (var i = 0; i < elements; i += 4)
						{
							dst[i + 0] = src[i + 2];
							dst[i + 1] = src[i + 1];
							dst[i + 2] = src[i + 0];
							dst[i + 3] = src[i + 3];
						}
					}
					else
					{
						for (var i = 0; i < elements; i += 3)
						{
							dst[i + 0] = src[i + 2];
							dst[i + 1] = src[i + 1];
							dst[i + 2] = src[i + 0];
						}
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
			Span<byte> sigBuffer = stackalloc byte[8];
			var isPng = s.Read(sigBuffer) == 8 && sigBuffer.SequenceEqual(Signature);
			s.Position = pos;
			return isPng;
		}

		[Flags]
		enum PngColorType : byte { Indexed = 1, Color = 2, Alpha = 4 }
		enum PngFilter : byte { None, Sub, Up, Average, Paeth }

		static bool IsPaletted(byte bitDepth, PngColorType colorType)
		{
			if (bitDepth <= 8 && colorType == (PngColorType.Indexed | PngColorType.Color))
				return true;

			if (bitDepth == 8 && colorType == (PngColorType.Color | PngColorType.Alpha))
				return false;

			if (bitDepth == 8 && colorType == PngColorType.Color)
				return false;

			throw new InvalidDataException("Unknown pixel format");
		}

		static void WritePngChunk(MemoryStream output, uint type, MemoryStream input)
		{
			Span<byte> header = stackalloc byte[8];
			BinaryPrimitives.WriteInt32BigEndian(header[..4], (int)input.Length);
			BinaryPrimitives.WriteUInt32BigEndian(header[4..], type);

			if (!input.TryGetBuffer(out var dataSegment))
				dataSegment = new ArraySegment<byte>(input.ToArray());

			ReadOnlySpan<byte> data = dataSegment.AsSpan(0, (int)input.Length);

			output.Write(header);
			output.Write(data);

			var crc = 0xFFFFFFFF;
			crc = CRC32.Update(crc, header[4..]);
			crc = CRC32.Update(crc, data);
			var finalCrc = CRC32.Finish(crc);

			output.Write(IPAddress.NetworkToHostOrder((int)finalCrc));
		}

		public byte[] Save(CompressionLevel compression = CompressionLevel.SmallestSize)
		{
			using (var output = new MemoryStream())
			{
				output.Write(Signature);
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

					WritePngChunk(output, ChunkIHDR, header);
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

						WritePngChunk(output, ChunkPLTE, palette);
					}
				}

				if (alphaPalette)
				{
					using (var alpha = new MemoryStream())
					{
						foreach (var c in Palette)
							alpha.WriteByte(c.A);

						WritePngChunk(output, ChunkTRNS, alpha);
					}
				}

				using (var data = new MemoryStream())
				{
					using (var compressed = new ZLibStream(data, compression, true))
					{
						var rowStride = Width * PixelStride;
						for (var y = 0; y < Height; y++)
						{
							// Assuming no filtering for simplicity
							const byte FilterType = 0;
							compressed.WriteByte(FilterType);
							compressed.Write(Data, y * rowStride, rowStride);
						}
					}

					WritePngChunk(output, ChunkIDAT, data);
				}

				foreach (var kv in EmbeddedData)
				{
					using (var text = new MemoryStream())
					{
						text.Write(Encoding.ASCII.GetBytes(kv.Key + (char)0 + kv.Value));
						WritePngChunk(output, ChunkTEXT, text);
					}
				}

				WritePngChunk(output, ChunkIEND, new MemoryStream());
				return output.ToArray();
			}
		}

		public void Save(string path, CompressionLevel compression = CompressionLevel.SmallestSize)
		{
			File.WriteAllBytes(path, Save(compression));
		}
	}
}
