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
using System.IO;
using OpenRA.Mods.Common.FileFormats;
using OpenRA.Video;

namespace OpenRA.Mods.Cnc.FileFormats
{
	public class VqaVideo : IVideo
	{
		public ushort FrameCount { get; }
		public byte Framerate { get; }
		public ushort Width { get; }
		public ushort Height { get; }

		public byte[] CurrentFrameData { get; }
		public int CurrentFrameIndex { get; private set; }

		public bool HasAudio { get; set; }
		public byte[] AudioData { get; private set; } // audio for this frame: 22050Hz 16bit mono pcm, uncompressed.
		public int AudioChannels { get; }
		public int SampleBits { get; }
		public int SampleRate { get; }

		readonly Stream stream;
		readonly ushort numColors;
		readonly ushort blockWidth;
		readonly ushort blockHeight;
		readonly byte chunkBufferParts;
		readonly int2 blocks;
		readonly uint[] offsets;
		readonly byte[] paletteBytes;
		readonly uint videoFlags; // if 0x10 is set the video is a 16 bit hq video (ts and later)
		readonly ushort totalFrameWidth;

		// Stores a list of subpixels, referenced by the VPTZ chunk
		byte[] cbf;
		readonly byte[] cbp;
		readonly byte[] cbfBuffer;
		bool cbpIsCompressed;

		// Buffer for loading file subchunks, the maximum chunk size of a file is not defined
		// and the header definition for the size of the biggest chunks (color data) isn't accurate.
		// But 256k is large enough for all TS videos(< 200k).
		readonly byte[] fileBuffer = new byte[256000];
		readonly int maxCbfzSize = 256000;
		int vtprSize = 0;
		int currentChunkBuffer = 0;
		int chunkBufferOffset = 0;

		// Top half contains block info, bottom half contains references to cbf array
		readonly byte[] origData;

		public VqaVideo(Stream stream, bool useFramePadding)
		{
			this.stream = stream;

			// Decode FORM chunk
			if (stream.ReadASCII(4) != "FORM")
				throw new InvalidDataException("Invalid vqa (invalid FORM section)");
			/*var length = */stream.ReadUInt32();

			if (stream.ReadASCII(8) != "WVQAVQHD")
				throw new InvalidDataException("Invalid vqa (not WVQAVQHD)");
			/*var length2 = */stream.ReadUInt32();

			/*var version = */stream.ReadUInt16();
			videoFlags = stream.ReadUInt16();
			FrameCount = stream.ReadUInt16();
			Width = stream.ReadUInt16();
			Height = stream.ReadUInt16();

			blockWidth = stream.ReadUInt8();
			blockHeight = stream.ReadUInt8();
			Framerate = stream.ReadUInt8();
			chunkBufferParts = stream.ReadUInt8();
			blocks = new int2(Width / blockWidth, Height / blockHeight);

			numColors = stream.ReadUInt16();
			/*var maxBlocks = */stream.ReadUInt16();
			/*var unknown1 = */stream.ReadUInt16();
			/*var unknown2 = */stream.ReadUInt32();

			// Audio
			SampleRate = stream.ReadUInt16();
			AudioChannels = stream.ReadByte();
			SampleBits = stream.ReadByte();

			/*var unknown3 =*/stream.ReadUInt32();
			/*var unknown4 =*/stream.ReadUInt16();
			/*maxCbfzSize =*/stream.ReadUInt32(); // Unreliable

			/*var unknown5 =*/stream.ReadUInt32();

			if (IsHqVqa)
			{
				cbfBuffer = new byte[maxCbfzSize];
				cbf = new byte[maxCbfzSize * 3];
				origData = new byte[maxCbfzSize];
			}
			else
			{
				cbfBuffer = new byte[Width * Height];
				cbf = new byte[Width * Height];
				cbp = new byte[Width * Height];
				origData = new byte[2 * blocks.X * blocks.Y];
			}

			paletteBytes = new byte[numColors * 4];
			var type = stream.ReadASCII(4);
			while (type != "FINF")
			{
				// Sub type is a file tag
				if (type[3] == 'F')
				{
					var jmp = int2.Swap(stream.ReadUInt32());
					stream.Seek(jmp, SeekOrigin.Current);
					type = stream.ReadASCII(4);
				}
				else
					throw new NotSupportedException($"Vqa uses unknown Subtype: {type}");
			}

			/*var length = */stream.ReadUInt16();
			/*var unknown4 = */stream.ReadUInt16();

			// Frame offsets
			offsets = new uint[FrameCount];
			for (var i = 0; i < FrameCount; i++)
			{
				offsets[i] = stream.ReadUInt32();
				if (offsets[i] > 0x40000000)
					offsets[i] -= 0x40000000;
				offsets[i] <<= 1;
			}

			CollectAudioData();

			if (useFramePadding)
			{
				var frameSize = Exts.NextPowerOf2(Math.Max(Width, Height));
				CurrentFrameData = new byte[frameSize * frameSize * 4];
				totalFrameWidth = (ushort)frameSize;
			}
			else
			{
				CurrentFrameData = new byte[Width * Height * 4];
				totalFrameWidth = Width;
			}

			Reset();
		}

		public void Reset()
		{
			CurrentFrameIndex = chunkBufferOffset = currentChunkBuffer = 0;
			LoadFrame();
		}

		void CollectAudioData()
		{
			var audio1 = new MemoryStream(); // left channel / mono
			var audio2 = new MemoryStream(); // right channel
			var adpcmIndex = 0;
			var compressed = false;
			for (var i = 0; i < FrameCount; i++)
			{
				stream.Seek(offsets[i], SeekOrigin.Begin);
				var end = (i < FrameCount - 1) ? offsets[i + 1] : stream.Length;

				while (stream.Position < end)
				{
					var type = stream.ReadASCII(4);
					if (type == "SN2J")
					{
						var jmp = int2.Swap(stream.ReadUInt32());
						stream.Seek(jmp, SeekOrigin.Current);
						type = stream.ReadASCII(4);
					}

					var length = int2.Swap(stream.ReadUInt32());

					switch (type)
					{
						case "SND0":
						case "SND2":
							if (AudioChannels == 0)
								throw new NotSupportedException();
							else if (AudioChannels == 1)
							{
								var rawAudio = stream.ReadBytes((int)length);
								audio1.WriteArray(rawAudio);
							}
							else
							{
								var rawAudio = stream.ReadBytes((int)length / 2);
								audio1.WriteArray(rawAudio);
								rawAudio = stream.ReadBytes((int)length / 2);
								audio2.WriteArray(rawAudio);
								if (length % 2 != 0)
									stream.ReadBytes(2);
							}

							compressed = type == "SND2";
							break;
						default:
							if (length + stream.Position > stream.Length)
								throw new NotSupportedException($"Vqa uses unknown Subtype: {type}");
							stream.ReadBytes((int)length);
							break;
					}

					// Chunks are aligned on even bytes; advance by a byte if the next one is null
					if (stream.Peek() == 0) stream.ReadByte();
				}
			}

			if (AudioChannels == 1)
				AudioData = compressed ? ImaAdpcmReader.LoadImaAdpcmSound(audio1.ToArray(), ref adpcmIndex) : audio1.ToArray();
			else
			{
				byte[] leftData, rightData;
				if (!compressed)
				{
					leftData = audio1.ToArray();
					rightData = audio2.ToArray();
				}
				else
				{
					adpcmIndex = 0;
					leftData = ImaAdpcmReader.LoadImaAdpcmSound(audio1.ToArray(), ref adpcmIndex);
					adpcmIndex = 0;
					rightData = ImaAdpcmReader.LoadImaAdpcmSound(audio2.ToArray(), ref adpcmIndex);
				}

				AudioData = new byte[rightData.Length + leftData.Length];
				var rightIndex = 0;
				var leftIndex = 0;
				for (var i = 0; i < AudioData.Length;)
				{
					AudioData[i++] = leftData[leftIndex++];
					AudioData[i++] = leftData[leftIndex++];
					AudioData[i++] = rightData[rightIndex++];
					AudioData[i++] = rightData[rightIndex++];
				}
			}

			HasAudio = AudioData.Length > 0;
		}

		public void AdvanceFrame()
		{
			CurrentFrameIndex++;
			LoadFrame();
		}

		void LoadFrame()
		{
			if (CurrentFrameIndex >= FrameCount)
				return;

			// Seek to the start of the frame
			stream.Seek(offsets[CurrentFrameIndex], SeekOrigin.Begin);
			var end = (CurrentFrameIndex < FrameCount - 1) ? offsets[CurrentFrameIndex + 1] : stream.Length;

			while (stream.Position < end)
			{
				var type = stream.ReadASCII(4);
				uint length;
				if (type == "SN2J")
				{
					var jmp = int2.Swap(stream.ReadUInt32());
					stream.Seek(jmp, SeekOrigin.Current);
					type = stream.ReadASCII(4);
					if (type == "SND2")
					{
						length = int2.Swap(stream.ReadUInt32());
						stream.Seek(length, SeekOrigin.Current);
						type = stream.ReadASCII(4);
					}
					else
						throw new NotSupportedException();
				}

				length = int2.Swap(stream.ReadUInt32());

				switch (type)
				{
					case "VQFR":
						DecodeVQFR(stream);
						break;
					case "\0VQF":
						stream.ReadByte();
						DecodeVQFR(stream);
						break;
					case "VQFL":
						DecodeVQFR(stream, "VQFL");
						break;
					default:
						// Don't parse sound here.
						stream.ReadBytes((int)length);
						break;
				}

				// Chunks are aligned on even bytes; advance by a byte if the next one is null
				if (stream.Peek() == 0) stream.ReadByte();
			}

			// Now that the frame data has been loaded (in the relevant private fields), decode it into CurrentFrameData.
			DecodeFrameData();
		}

		// VQA Frame
		public void DecodeVQFR(Stream s, string parentType = "VQFR")
		{
			// The CBP chunks each contain 1/8th of the full lookup table
			// Annoyingly, the complete table is not applied until the frame
			// *after* the one that contains the 8th chunk.
			// Do we have a set of partial lookup tables ready to apply?
			if (currentChunkBuffer == chunkBufferParts && chunkBufferParts != 0)
			{
				if (!cbpIsCompressed)
					cbf = (byte[])cbp.Clone();
				else
					LCWDecodeInto(cbp, cbf);

				chunkBufferOffset = currentChunkBuffer = 0;
			}

			while (true)
			{
				// Chunks are aligned on even bytes; may be padded with a single null
				if (s.Peek() == 0) s.ReadByte();
				var type = s.ReadASCII(4);
				var subchunkLength = (int)int2.Swap(s.ReadUInt32());

				switch (type)
				{
					// Full frame-modifier
					case "CBFZ":
						var decodeMode = s.Peek() == 0;
						s.ReadBytes(fileBuffer, 0, subchunkLength);
						Array.Clear(cbf, 0, cbf.Length);
						Array.Clear(cbfBuffer, 0, cbfBuffer.Length);
						var decodeCount = LCWDecodeInto(fileBuffer, cbfBuffer, decodeMode ? 1 : 0, decodeMode);
						if ((videoFlags & 0x10) == 16)
						{
							var p = 0;
							for (var i = 0; i < decodeCount; i += 2)
							{
								var packed = cbfBuffer[i + 1] << 8 | cbfBuffer[i];
								/* 15      bit      0
								   0rrrrrgg gggbbbbb
								   HI byte  LO byte*/
								cbf[p++] = (byte)((packed & 0x7C00) >> 7);
								cbf[p++] = (byte)((packed & 0x3E0) >> 2);
								cbf[p++] = (byte)((packed & 0x1f) << 3);
							}
						}
						else
						{
							cbf = cbfBuffer;
						}

						if (parentType == "VQFL")
							return;
						break;
					case "CBF0":
						cbf = s.ReadBytes(subchunkLength);
						break;

					// frame-modifier chunk
					case "CBP0":
					case "CBPZ":
						var bytes = s.ReadBytes(subchunkLength);
						bytes.CopyTo(cbp, chunkBufferOffset);
						chunkBufferOffset += subchunkLength;
						currentChunkBuffer++;
						cbpIsCompressed = type == "CBPZ";
						break;

					// Palette
					case "CPL0":
						for (var i = 0; i < numColors; i++)
						{
							var r = (byte)(s.ReadUInt8() << 2);
							var g = (byte)(s.ReadUInt8() << 2);
							var b = (byte)(s.ReadUInt8() << 2);
							paletteBytes[i * 4] = b;
							paletteBytes[i * 4 + 1] = g;
							paletteBytes[i * 4 + 2] = r;
							paletteBytes[i * 4 + 3] = 255;
						}

						break;

					// Frame data
					case "VPTZ":
						LCWDecodeInto(s.ReadBytes(subchunkLength), origData);

						// This is the last subchunk
						return;
					case "VPRZ":
						Array.Clear(origData, 0, origData.Length);
						s.ReadBytes(fileBuffer, 0, subchunkLength);
						if (fileBuffer[0] != 0)
							vtprSize = LCWDecodeInto(fileBuffer, origData);
						else
							LCWDecodeInto(fileBuffer, origData, 1, true);
						return;
					case "VPTR":
						Array.Clear(origData, 0, origData.Length);
						s.ReadBytes(origData, 0, subchunkLength);
						vtprSize = subchunkLength;
						return;
					default:
						throw new InvalidDataException($"Unknown sub-chunk {type}");
				}
			}
		}

		void DecodeFrameData()
		{
			if (IsHqVqa)
			{
				/* The VP?? chunks of the video file contains an array of instructions for
				 * how the blocks of the finished frame will be filled with color data blocks
				 * contained in the CBF? chunks.
				 */
				var p = 0;
				for (var y = 0; y < blocks.Y;)
				{
					for (var x = 0; x < blocks.X;)
					{
						if (y >= blocks.Y)
							break;

						// The first 3 bits of the short determine the type of instruction with the rest being one or two parameters.
						var val = (int)origData[p++];
						val |= origData[p++] << 8;
						var para_A = val & 0x1fff;
						var para_B1 = val & 0xFF;
						var para_B2 = (((val / 256) & 0x1f) + 1) * 2;
						switch (val >> 13)
						{
							case 0:
								x += para_A;
								break;
							case 1:
								WriteBlock(para_B1, para_B2, ref x, ref y);
								break;
							case 2:
								WriteBlock(para_B1, 1, ref x, ref y);
								for (var i = 0; i < para_B2; i++)
									WriteBlock(origData[p++], 1, ref x, ref y);
								break;
							case 3:
								WriteBlock(para_A, 1, ref x, ref y);
								break;
							case 5:
								WriteBlock(para_A, origData[p++], ref x, ref y);
								break;
							default:
								throw new NotSupportedException();
						}
					}

					y++;
				}

				if (p != vtprSize)
					throw new IndexOutOfRangeException();
			}
			else
			{
				for (var y = 0; y < blocks.Y; y++)
				{
					for (var x = 0; x < blocks.X; x++)
					{
						var px = origData[x + y * blocks.X];
						var mod = origData[x + (y + blocks.Y) * blocks.X];
						for (var j = 0; j < blockHeight; j++)
						{
							for (var i = 0; i < blockWidth; i++)
							{
								var cbfi = (mod * 256 + px) * 8 + j * blockWidth + i;
								var colorIndex = (mod == 0x0f) ? px : cbf[cbfi];

								var pixelX = x * blockWidth + i;
								var pixelY = y * blockHeight + j;
								var pos = pixelY * totalFrameWidth + pixelX;
								CurrentFrameData[pos * 4] = paletteBytes[colorIndex * 4];
								CurrentFrameData[pos * 4 + 1] = paletteBytes[colorIndex * 4 + 1];
								CurrentFrameData[pos * 4 + 2] = paletteBytes[colorIndex * 4 + 2];
								CurrentFrameData[pos * 4 + 3] = paletteBytes[colorIndex * 4 + 3];
							}
						}
					}
				}
			}
		}

		bool IsHqVqa => (videoFlags & 0x10) == 16;

		void WriteBlock(int blockNumber, int count, ref int x, ref int y)
		{
			for (var i = 0; i < count; i++)
			{
				var offset = blockNumber * blockHeight * blockWidth * 3;
				for (var by = 0; by < blockHeight; by++)
					for (var bx = 0; bx < blockWidth; bx++)
					{
						var p = (bx + by * blockWidth) * 3;

						var pixelX = x * blockWidth + bx;
						var pixelY = y * blockHeight + by;
						var pos = pixelY * totalFrameWidth + pixelX;
						CurrentFrameData[pos * 4] = cbf[offset + p + 2];
						CurrentFrameData[pos * 4 + 1] = cbf[offset + p + 1];
						CurrentFrameData[pos * 4 + 2] = cbf[offset + p];
						CurrentFrameData[pos * 4 + 3] = 255;
					}

				x++;
				if (x >= blocks.X)
				{
					x = 0;
					y++;
					if (y >= blocks.Y && i != count - 1)
						throw new IndexOutOfRangeException();
				}
			}
		}

		// TODO: Maybe replace this with LCWCompression.DecodeInto again later
		public static int LCWDecodeInto(byte[] src, byte[] dest, int srcOffset = 0, bool reverse = false)
		{
			var ctx = new FastByteReader(src, srcOffset);
			var destIndex = 0;
			while (true)
			{
				var i = ctx.ReadByte();
				if ((i & 0x80) == 0)
				{
					// case 2
					var secondByte = ctx.ReadByte();
					var count = ((i & 0x70) >> 4) + 3;
					var rpos = ((i & 0xf) << 8) + secondByte;

					if (destIndex + count > dest.Length)
						return destIndex;

					// Replicate previous
					var srcIndex = destIndex - rpos;
					if (srcIndex > destIndex)
						throw new NotImplementedException($"srcIndex > destIndex {srcIndex} {destIndex}");

					for (var j = 0; j < count; j++)
					{
						if (destIndex - srcIndex == 1)
							dest[destIndex + j] = dest[destIndex - 1];
						else
							dest[destIndex + j] = dest[srcIndex + j];
					}

					destIndex += count;
				}
				else if ((i & 0x40) == 0)
				{
					// case 1
					var count = i & 0x3F;
					if (count == 0)
						return destIndex;

					ctx.CopyTo(dest, destIndex, count);
					destIndex += count;
				}
				else
				{
					var count3 = i & 0x3F;
					if (count3 == 0x3E)
					{
						// case 4
						var count = ctx.ReadWord();
						var color = ctx.ReadByte();

						for (var end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = color;
					}
					else
					{
						// If count3 == 0x3F it's case 5, else case 3
						var count = count3 == 0x3F ? ctx.ReadWord() : count3 + 3;
						var srcIndex = reverse ? destIndex - ctx.ReadWord() : ctx.ReadWord();
						if (srcIndex >= destIndex)
							throw new NotImplementedException($"srcIndex >= destIndex {srcIndex} {destIndex}");

						for (var end = destIndex + count; destIndex < end; destIndex++)
							dest[destIndex] = dest[srcIndex++];
					}
				}
			}
		}
	}
}
