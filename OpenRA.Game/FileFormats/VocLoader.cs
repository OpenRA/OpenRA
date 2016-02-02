#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OpenRA.FileFormats
{
	public class VocLoader : ISoundLoader
	{
		bool ISoundLoader.CanParse(Stream stream)
		{
			var position = stream.Position;
			try
			{
				VocStream.CheckVocHeader(stream);
			}
			catch
			{
				return false;
			}
			finally
			{
				stream.Position = position;
			}

			return true;
		}

		bool ISoundLoader.TryParseSound(Stream stream, string fileName, out byte[] rawData, out int channels, out int sampleBits, out int sampleRate)
		{
			var position = stream.Position;

			try
			{
				var vocStream = new VocStream(stream);
				channels = vocStream.Channels;
				sampleBits = vocStream.BitsPerSample;
				sampleRate = vocStream.SampleRate;
				rawData = vocStream.ReadAllBytes();
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to parse VOC file {0}. Error message:".F(fileName));
				Log.Write("debug", e.ToString());

				rawData = null;
				channels = sampleBits = sampleRate = 0;
				return false;
			}
			finally
			{
				stream.Position = position;
			}

			return true;
		}

		float ISoundLoader.GetLength(Stream stream)
		{
			try
			{
				var vocStream = new VocStream(stream);
				return vocStream.LengthInSeconds;
			}
			catch (Exception e)
			{
				Log.Write("debug", "Failed to parse VOC file. Error message:");
				Log.Write("debug", e.ToString());
			}

			return 0;
		}
	}

	public class VocStream : Stream
	{
		public int BitsPerSample { get { return 8; } }
		public int Channels { get { return 1; } }
		public int SampleRate { get; private set; }
		public float LengthInSeconds { get { return (float)totalSamples / SampleRate; } }

		int totalSamples = 0;
		int samplePosition = 0;

		Stream stream;
		List<VocBlock> blocks = new List<VocBlock>();
		IEnumerator<VocBlock> currentBlock;
		int samplesLeftInBlock = 0;
		byte[] buffer = new byte[4096];

		struct VocFileHeader
		{
			public string Description;
			public int DatablockOffset;
			public int Version;
			public int ID;

			public static VocFileHeader Read(Stream s)
			{
				VocFileHeader vfh;
				vfh.Description = s.ReadASCII(20);
				vfh.DatablockOffset = s.ReadUInt16();
				vfh.Version = s.ReadUInt16();
				vfh.ID = s.ReadUInt16();
				return vfh;
			}
		}

		struct VocBlock
		{
			public int Code;
			public int Length;
			public VocSampleBlock SampleBlock;
			public VocLoopBlock LoopBlock;
		}

		struct VocSampleBlock
		{
			public int Rate;
			public int Samples;
			public long Offset;
		}

		struct VocLoopBlock
		{
			public int Count;
		}

		public VocStream(Stream stream)
		{
			this.stream = stream;
			CheckVocHeader(stream);
			Preload();
		}

		public static void CheckVocHeader(Stream stream)
		{
			var vfh = VocFileHeader.Read(stream);

			if (!vfh.Description.StartsWith("Creative Voice File"))
				throw new InvalidDataException("Voc header description not recognized");
			if (vfh.DatablockOffset != 26)
				throw new InvalidDataException("Voc header offset is wrong");
			if (vfh.Version != 0x010A)
				throw new InvalidDataException("Voc header version not recognized");
			if (vfh.ID != ~vfh.Version + 0x1234)
				throw new InvalidDataException("Voc header id is bogus - expected: " +
					(~vfh.Version + 0x1234).ToString("X") + " but value is : " + vfh.ID.ToString("X"));
		}

		int GetSampleRateFromVocRate(int vocSampleRate)
		{
			if (vocSampleRate == 256)
				throw new InvalidDataException("Invalid frequency divisor 256 in voc file");
			if (vocSampleRate == 0xa5 || vocSampleRate == 0xa6)
				return 11025;
			else if (vocSampleRate == 0xd2 || vocSampleRate == 0xd3)
				return 22050;
			else
				return (int)(1000000L / (256L - vocSampleRate));
		}

		void Preload()
		{
			while (true)
			{
				VocBlock block = new VocBlock();
				try
				{
					block.Code = stream.ReadByte();
					block.Length = 0;
				}
				catch (EndOfStreamException)
				{
					// Stream is allowed to end without a last block
					break;
				}

				if (block.Code == 0 || block.Code > 9)
					break;

				block.Length = stream.ReadByte();
				block.Length |= stream.ReadByte() << 8;
				block.Length |= stream.ReadByte() << 16;

				var skip = 0;
				switch (block.Code)
				{
					// Sound data
					case 1:
						{
							if (block.Length < 2)
								throw new InvalidDataException("Invalid sound data block length in voc file");
							var freqDiv = stream.ReadByte();
							block.SampleBlock.Rate = GetSampleRateFromVocRate(freqDiv);
							var codec = stream.ReadByte();
							if (codec != 0)
								throw new InvalidDataException("Unhandled codec used in voc file");
							skip = block.Length - 2;
							block.SampleBlock.Samples = skip;
							block.SampleBlock.Offset = stream.Position;

							// See if last block contained additional information
							if (blocks.Count > 0)
							{
								var b = blocks.Last();
								if (b.Code == 8)
								{
									block.SampleBlock.Rate = b.SampleBlock.Rate;
									blocks.Remove(b);
								}
							}

							SampleRate = Math.Max(SampleRate, block.SampleBlock.Rate);
							break;
						}

						// Silence
					case 3:
						{
							if (block.Length != 3)
								throw new InvalidDataException("Invalid silence block length in voc file");
							block.SampleBlock.Offset = 0;
							block.SampleBlock.Samples = stream.ReadUInt16() + 1;
							var freqDiv = stream.ReadByte();
							block.SampleBlock.Rate = GetSampleRateFromVocRate(freqDiv);
							break;
						}

						// Repeat start
					case 6:
						{
							if (block.Length != 2)
								throw new InvalidDataException("Invalid repeat start block length in voc file");
							block.LoopBlock.Count = stream.ReadUInt16() + 1;
							break;
						}

						// Repeat end
					case 7:
						break;

						// Extra info
					case 8:
						{
							if (block.Length != 4)
								throw new InvalidDataException("Invalid info block length in voc file");
							int freqDiv = stream.ReadUInt16();
							if (freqDiv == 65536)
								throw new InvalidDataException("Invalid frequency divisor 65536 in voc file");
							var codec = stream.ReadByte();
							if (codec != 0)
								throw new InvalidDataException("Unhandled codec used in voc file");
							var channels = stream.ReadByte() + 1;
							if (channels != 1)
								throw new InvalidDataException("Unhandled number of channels in voc file");
							block.SampleBlock.Offset = 0;
							block.SampleBlock.Samples = 0;
							block.SampleBlock.Rate = (int)(256000000L / (65536L - freqDiv));
							break;
						}

					// Sound data (New format)
					case 9:
					default:
						throw new InvalidDataException("Unhandled code in voc file");
				}

				if (skip > 0)
					stream.Seek(skip, SeekOrigin.Current);
				blocks.Add(block);
			}

			// Check validity and calculated total number of samples
			foreach (var b in blocks)
			{
				if (b.Code == 8)
					throw new InvalidDataException("Unused block 8 in voc file");
				if (b.Code != 1 && b.Code != 9)
					continue;
				if (b.SampleBlock.Rate != SampleRate)
					throw new InvalidDataException("Voc file contains chunks with different sample rate");
				totalSamples += b.SampleBlock.Samples;
			}

			Rewind();
		}

		void Rewind()
		{
			currentBlock = blocks.GetEnumerator();
			samplesLeftInBlock = 0;
			samplePosition = 0;

			while (currentBlock.MoveNext())
			{
				if (currentBlock.Current.Code == 1)
				{
					stream.Seek(currentBlock.Current.SampleBlock.Offset, SeekOrigin.Begin);
					samplesLeftInBlock = currentBlock.Current.SampleBlock.Samples;
					return;
				}
			}
		}

		bool EndOfData { get { return currentBlock.Current.Equals(blocks.Last()) && samplesLeftInBlock == 0; } }

		int FillBuffer(int maxSamples)
		{
			var bufferedSamples = 0;
			var offset = 0;

			maxSamples = Math.Min(buffer.Length, maxSamples);

			while (maxSamples > 0 && !EndOfData)
			{
				var len = Math.Min(maxSamples, samplesLeftInBlock);
				stream.ReadBytes(buffer, offset, len);
				offset += len;
				var samplesRead = len;
				bufferedSamples += samplesRead;
				maxSamples -= samplesRead;
				samplesLeftInBlock -= samplesRead;
				samplePosition += len;

				UpdateBlockIfNeeded();
			}

			return bufferedSamples;
		}

		void UpdateBlockIfNeeded()
		{
			if (samplesLeftInBlock == 0)
			{
				while (currentBlock.MoveNext())
				{
					if (currentBlock.Current.Code != 1 && currentBlock.Current.Code != 9)
						continue;
					stream.Seek(currentBlock.Current.SampleBlock.Offset, SeekOrigin.Begin);
					samplesLeftInBlock = currentBlock.Current.SampleBlock.Samples;
					return;
				}
			}
		}

		public byte[] ReadAllBytes()
		{
			Rewind();
			var buffer = new byte[totalSamples];
			Read(buffer, 0, totalSamples);
			return buffer;
		}

		public override bool CanRead { get { return true; } }

		public override bool CanSeek { get { return false; } }

		public override bool CanWrite { get { return false; } }

		public override long Length { get { return totalSamples; } }

		public override long Position
		{
			get { return samplePosition; }
			set { throw new NotImplementedException(); }
		}

		public override void Flush()
		{
			throw new NotImplementedException();
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var bytesWritten = 0;
			var samplesLeft = Math.Min(count, buffer.Length - offset);
			while (samplesLeft > 0)
			{
				var len = FillBuffer(samplesLeft);
				if (len == 0)
					break;
				Array.Copy(this.buffer, 0, buffer, offset, len);
				samplesLeft -= len;
				offset += len;
				bytesWritten += len;
			}

			return bytesWritten;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}
	}
}
