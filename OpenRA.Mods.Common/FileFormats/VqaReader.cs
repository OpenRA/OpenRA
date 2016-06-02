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
using System.IO;
using OpenRA.FileFormats;

namespace OpenRA.Mods.Common.FileFormats
{
	public class VqaReader
	{
		public readonly ushort Frames;
		public readonly byte Framerate;
		public readonly ushort Width;
		public readonly ushort Height;

		Stream stream;
		int currentFrame;
		ushort numColors;
		ushort blockWidth;
		ushort blockHeight;
		byte chunkBufferParts;
		int2 blocks;
		uint[] offsets;
		uint[] palette;
		uint videoFlags; // if 0x10 is set the video is a 16 bit hq video (ts and later)
		int sampleRate;
		int sampleBits;
		int audioChannels;

		// Stores a list of subpixels, referenced by the VPTZ chunk
		byte[] cbf;
		byte[] cbp;
		byte[] cbfBuffer;

		// Buffer for loading file subchunks, the maximum chunk size of a file is not defined
		// and the header definition for the size of the biggest chunks (color data) isn't accurate.
		// But 256k is large enough for all TS videos(< 200k).
		byte[] fileBuffer = new byte[256000];
		int maxCbfzSize = 256000;
		int vtprSize = 0;
		int currentChunkBuffer = 0;
		int chunkBufferOffset = 0;

		// Top half contains block info, bottom half contains references to cbf array
		byte[] origData;

		// Final frame output
		uint[,] frameData;
		byte[] audioData;		// audio for this frame: 22050Hz 16bit mono pcm, uncompressed.
		bool hasAudio;

		public byte[] AudioData { get { return audioData; } }
		public int CurrentFrame { get { return currentFrame; } }
		public int SampleRate { get { return sampleRate; } }
		public int SampleBits { get { return sampleBits; } }
		public int AudioChannels { get { return audioChannels; } }
		public bool HasAudio { get { return hasAudio; } }

		public VqaReader(Stream stream)
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
			Frames = stream.ReadUInt16();
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
			sampleRate = stream.ReadUInt16();
			audioChannels = stream.ReadByte();
			sampleBits = stream.ReadByte();

			/*var unknown3 =*/stream.ReadUInt32();
			/*var unknown4 =*/stream.ReadUInt16();
			/*maxCbfzSize =*/stream.ReadUInt32(); // Unreliable

			/*var unknown5 =*/stream.ReadUInt32();

			var frameSize = Exts.NextPowerOf2(Math.Max(Width, Height));

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

			palette = new uint[numColors];
			frameData = new uint[frameSize, frameSize];
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
					throw new NotSupportedException("Vqa uses unknown Subtype: {0}".F(type));
			}

			/*var length = */stream.ReadUInt16();
			/*var unknown4 = */stream.ReadUInt16();

			// Frame offsets
			offsets = new uint[Frames];
			for (var i = 0; i < Frames; i++)
			{
				offsets[i] = stream.ReadUInt32();
				if (offsets[i] > 0x40000000)
					offsets[i] -= 0x40000000;
				offsets[i] <<= 1;
			}

			CollectAudioData();

			Reset();
		}

		public void Reset()
		{
			currentFrame = chunkBufferOffset = currentChunkBuffer = 0;
			LoadFrame();
		}

		void CollectAudioData()
		{
			var audio1 = new MemoryStream(); // left channel / mono
			var audio2 = new MemoryStream(); // right channel
			var adpcmIndex = 0;
			var compressed = false;
			for (var i = 0; i < Frames; i++)
			{
				stream.Seek(offsets[i], SeekOrigin.Begin);
				var end = (i < Frames - 1) ? offsets[i + 1] : stream.Length;

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
							if (audioChannels == 0)
								throw new NotSupportedException();
							else if (audioChannels == 1)
							{
								var rawAudio = stream.ReadBytes((int)length);
								audio1.Write(rawAudio);
							}
							else
							{
								var rawAudio = stream.ReadBytes((int)length / 2);
								audio1.Write(rawAudio);
								rawAudio = stream.ReadBytes((int)length / 2);
								audio2.Write(rawAudio);
								if (length % 2 != 0)
									stream.ReadBytes(2);
							}

							compressed = type == "SND2";
							break;
						default:
							if (length + stream.Position > stream.Length)
								throw new NotSupportedException("Vqa uses unknown Subtype: {0}".F(type));
							stream.ReadBytes((int)length);
							break;
					}

					// Chunks are aligned on even bytes; advance by a byte if the next one is null
					if (stream.Peek() == 0) stream.ReadByte();
				}
			}

			if (audioChannels == 1)
				audioData = compressed ? AudReader.LoadSound(audio1.ToArray(), ref adpcmIndex) : audio1.ToArray();
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
					leftData = AudReader.LoadSound(audio1.ToArray(), ref adpcmIndex);
					adpcmIndex = 0;
					rightData = AudReader.LoadSound(audio2.ToArray(), ref adpcmIndex);
				}

				audioData = new byte[rightData.Length + leftData.Length];
				var rightIndex = 0;
				var leftIndex = 0;
				for (var i = 0; i < audioData.Length;)
				{
					audioData[i++] = leftData[leftIndex++];
					audioData[i++] = leftData[leftIndex++];
					audioData[i++] = rightData[rightIndex++];
					audioData[i++] = rightData[rightIndex++];
				}
			}

			hasAudio = audioData.Length > 0;
		}

		public void AdvanceFrame()
		{
			currentFrame++;
			LoadFrame();
		}

		void LoadFrame()
		{
			if (currentFrame >= Frames)
				return;

			// Seek to the start of the frame
			stream.Seek(offsets[currentFrame], SeekOrigin.Begin);
			var end = (currentFrame < Frames - 1) ? offsets[currentFrame + 1] : stream.Length;

			while (stream.Position < end)
			{
				var type = stream.ReadASCII(4);
				var length = 0U;
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
		}

		// VQA Frame
		public void DecodeVQFR(Stream s, string parentType = "VQFR")
		{
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
						var decodeCount = 0;
						decodeCount = LCWCompression.DecodeInto(fileBuffer, cbfBuffer, decodeMode ? 1 : 0, decodeMode);
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
						// Partial buffer is full; dump and recreate
						if (currentChunkBuffer == chunkBufferParts)
						{
							if (type == "CBP0")
								cbf = (byte[])cbp.Clone();
							else
								LCWCompression.DecodeInto(cbp, cbf);

							chunkBufferOffset = currentChunkBuffer = 0;
						}

						var bytes = s.ReadBytes(subchunkLength);
						bytes.CopyTo(cbp, chunkBufferOffset);
						chunkBufferOffset += subchunkLength;
						currentChunkBuffer++;
						break;

					// Palette
					case "CPL0":
						for (var i = 0; i < numColors; i++)
						{
							var r = (byte)(s.ReadUInt8() << 2);
							var g = (byte)(s.ReadUInt8() << 2);
							var b = (byte)(s.ReadUInt8() << 2);
							palette[i] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
						}

						break;

					// Frame data
					case "VPTZ":
						LCWCompression.DecodeInto(s.ReadBytes(subchunkLength), origData);

						// This is the last subchunk
						return;
					case "VPRZ":
						Array.Clear(origData, 0, origData.Length);
						s.ReadBytes(fileBuffer, 0, subchunkLength);
						if (fileBuffer[0] != 0)
							vtprSize = LCWCompression.DecodeInto(fileBuffer, origData);
						else
							LCWCompression.DecodeInto(fileBuffer, origData, 1, true);
						return;
					case "VPTR":
						Array.Clear(origData, 0, origData.Length);
						s.ReadBytes(origData, 0, subchunkLength);
						vtprSize = subchunkLength;
						return;
					default:
						throw new InvalidDataException("Unknown sub-chunk {0}".F(type));
				}
			}
		}

		int cachedFrame = -1;

		void DecodeFrameData()
		{
			cachedFrame = currentFrame;
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
								var color = (mod == 0x0f) ? px : cbf[cbfi];
								frameData[y * blockHeight + j, x * blockWidth + i] = palette[color];
							}
						}
					}
				}
			}
		}

		public uint[,] FrameData
		{
			get
			{
				if (cachedFrame != currentFrame)
					DecodeFrameData();

				return frameData;
			}
		}

		bool IsHqVqa { get { return (videoFlags & 0x10) == 16; } }

		void WriteBlock(int blockNumber, int count, ref int x, ref int y)
		{
			for (var i = 0; i < count; i++)
			{
				var frameX = x * blockWidth;
				var frameY = y * blockHeight;
				var offset = blockNumber * blockHeight * blockWidth * 3;
				for (var by = 0; by < blockHeight; by++)
					for (var bx = 0; bx < blockWidth; bx++)
					{
						var p = (bx + by * blockWidth) * 3;

						frameData[frameY + by, frameX + bx] = (uint)(0xFF << 24 | cbf[offset + p] << 16 | cbf[offset + p + 1] << 8 | cbf[offset + p + 2]);
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
	}
}
