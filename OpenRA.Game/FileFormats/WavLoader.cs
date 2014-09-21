#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;

namespace OpenRA.FileFormats
{
	public class WavLoader
	{
		public readonly int FileSize;
		public readonly string Format;

		public readonly int FmtChunkSize;
		public readonly int AudioFormat;
		public readonly int Channels;
		public readonly int SampleRate;
		public readonly int ByteRate;
		public readonly int BlockAlign;
		public readonly int BitsPerSample;

		public readonly int UncompressedSize;
		public readonly int DataSize;
		public readonly byte[] RawOutput;

		public enum WaveType { Pcm = 0x1, ImaAdpcm = 0x11 };
		public static WaveType Type { get; private set; }
		
		public WavLoader(Stream s)
		{
			while (s.Position < s.Length)
			{
				if ((s.Position & 1) == 1)
					s.ReadByte(); // Alignment

				var type = s.ReadASCII(4);
				switch (type)
				{
					case "RIFF":
						FileSize = s.ReadInt32();
						Format = s.ReadASCII(4);
						if (Format != "WAVE")
							throw new NotSupportedException("Not a canonical WAVE file.");
						break;
					case "fmt ":
						FmtChunkSize = s.ReadInt32();
						AudioFormat = s.ReadInt16();
						Type = (WaveType)AudioFormat;
						if (Type != WaveType.Pcm && Type != WaveType.ImaAdpcm)
							throw new NotSupportedException("Compression type is not supported.");
						Channels = s.ReadInt16();
						SampleRate = s.ReadInt32();
						ByteRate = s.ReadInt32();
						BlockAlign = s.ReadInt16();
						BitsPerSample = s.ReadInt16();

						s.ReadBytes(FmtChunkSize - 16);
						break;
					case "fact":
						{
							var chunkSize = s.ReadInt32();
							UncompressedSize = s.ReadInt32();
							s.ReadBytes(chunkSize - 4);
						}
						break;
					case "data":
						DataSize = s.ReadInt32();
						RawOutput = s.ReadBytes(DataSize);
						break;
					default:
						// Ignore unknown chunks
						{
							var chunkSize = s.ReadInt32();
							s.ReadBytes(chunkSize);
						}
						break;
				}
			}

			if (Type == WaveType.ImaAdpcm)
			{
				RawOutput = DecodeImaAdpcmData();
				BitsPerSample = 16;
			}
		}
		
		public static float WaveLength(Stream s)
		{
			s.Position = 12;
			var fmt = s.ReadASCII(4);

			if (fmt != "fmt ")
				return 0;

			s.Position = 22;
			var channels = s.ReadInt16();
			var sampleRate = s.ReadInt32();

			s.Position = 34;
			var bitsPerSample = s.ReadInt16();
			var length = s.Length * 8;

			return length / (channels * sampleRate * bitsPerSample);
		}

		public byte[] DecodeImaAdpcmData()
		{
			var s = new MemoryStream(RawOutput);

			var numBlocks = DataSize / BlockAlign;
			var blockDataSize = BlockAlign - (Channels * 4);
			var outputSize = UncompressedSize * Channels * 2;

			var outOffset = 0;
			var output = new byte[outputSize];

			var predictor = new int[Channels];
			var index = new int[Channels];

			// Decode each block of IMA ADPCM data in RawOutput
			for (var block = 0; block < numBlocks; block++)
			{
				// Each block starts with a initial state per-channel 
				for (var c = 0; c < Channels; c++)
				{
					predictor[c] = s.ReadInt16();
					index[c] = s.ReadUInt8();
					/* unknown/reserved */ s.ReadUInt8();

					// Output first sample from input
					output[outOffset++] = (byte)predictor[c];
					output[outOffset++] = (byte)(predictor[c] >> 8);

					if (outOffset >= outputSize)
						return output;
				}

				// Decode and output remaining data in this block
				var blockOffset = 0;
				while (blockOffset < blockDataSize)
				{
					for (var c = 0; c < Channels; c++)
					{
						// Decode 4 bytes (to 16 bytes of output) per channel
						var chunk = s.ReadBytes(4);
						var decoded = ImaAdpcmLoader.LoadImaAdpcmSound(chunk, ref index[c], ref predictor[c]);

						// Interleave output, one sample per channel
						var outOffsetChannel = outOffset + (2 * c);
						for (var i = 0; i < decoded.Length; i += 2)
						{
							var outOffsetSample = outOffsetChannel + i;
							if (outOffsetSample >= outputSize)
								return output;

							output[outOffsetSample] = decoded[i];
							output[outOffsetSample + 1] = decoded[i + 1];
							outOffsetChannel += 2 * (Channels - 1);
						}

						blockOffset += 4;
					}

					outOffset += 16 * Channels;
				}
			}

			return output;
		}
	}
}