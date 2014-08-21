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
		byte cbParts;
		int2 blocks;
		UInt32[] offsets;
		uint[] palette;

		// Stores a list of subpixels, referenced by the VPTZ chunk
		byte[] cbf;
		byte[] cbp;
		int cbChunk = 0;
		int cbOffset = 0;

		// Top half contains block info, bottom half contains references to cbf array
		byte[] origData;

		// Final frame output
		uint[,] frameData;
		byte[] audioData;		// audio for this frame: 22050Hz 16bit mono pcm, uncompressed.

		public byte[] AudioData { get { return audioData; } }
		public int CurrentFrame { get { return currentFrame; } }

		public VqaReader(Stream stream)
		{
			this.stream = stream;

			// Decode FORM chunk
			if (stream.ReadASCII(4) != "FORM")
				throw new InvalidDataException("Invalid vqa (invalid FORM section)");
			/*var length = */ stream.ReadUInt32();

			if (stream.ReadASCII(8) != "WVQAVQHD")
				throw new InvalidDataException("Invalid vqa (not WVQAVQHD)");
			/* var length = */stream.ReadUInt32();

			/*var version = */stream.ReadUInt16();
			/*var flags = */stream.ReadUInt16();
			Frames = stream.ReadUInt16();
			Width = stream.ReadUInt16();
			Height = stream.ReadUInt16();

			blockWidth = stream.ReadUInt8();
			blockHeight = stream.ReadUInt8();
			Framerate = stream.ReadUInt8();
			cbParts = stream.ReadUInt8();
			blocks = new int2(Width / blockWidth, Height / blockHeight);

			numColors = stream.ReadUInt16();
			/*var maxBlocks = */stream.ReadUInt16();
			/*var unknown1 = */stream.ReadUInt16();
			/*var unknown2 = */stream.ReadUInt32();

			// Audio
			/*var freq = */stream.ReadUInt16();
			/*var channels = */stream.ReadByte();
			/*var bits = */stream.ReadByte();
			/*var unknown3 = */stream.ReadBytes(14);

			var frameSize = Exts.NextPowerOf2(Math.Max(Width, Height));
			cbf = new byte[Width*Height];
			cbp = new byte[Width*Height];
			palette = new uint[numColors];
			origData = new byte[2*blocks.X*blocks.Y];
			frameData = new uint[frameSize, frameSize];

			var type = stream.ReadASCII(4);
			if (type != "FINF")
			{
				stream.Seek(27, SeekOrigin.Current);
				type = stream.ReadASCII(4);
			}

			/*var length = */stream.ReadUInt16();
			/*var unknown4 = */stream.ReadUInt16();

			// Frame offsets
			offsets = new UInt32[Frames];
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
			currentFrame = cbOffset = cbChunk = 0;
			LoadFrame();
		}

		void CollectAudioData()
		{
			var ms = new MemoryStream();
			var adpcmIndex = 0;

			var compressed = false;
			for (var i = 0; i < Frames; i++)
			{
				stream.Seek(offsets[i], SeekOrigin.Begin);
				var end = (i < Frames - 1) ? offsets[i + 1] : stream.Length;

				while (stream.Position < end)
				{
					var type = stream.ReadASCII(4);
					var length = int2.Swap(stream.ReadUInt32());

					switch (type)
					{
						case "SND0":
						case "SND2":
							var rawAudio = stream.ReadBytes((int)length);
							ms.Write(rawAudio);
							compressed = (type == "SND2");
							break;
						default:
							stream.ReadBytes((int)length);
							break;
					}

					// Chunks are aligned on even bytes; advance by a byte if the next one is null
					if (stream.Peek() == 0) stream.ReadByte();
				}
			}

			audioData = (compressed) ? AudLoader.LoadSound(ms.ToArray(), ref adpcmIndex) : ms.ToArray();
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
			var end = (currentFrame < Frames - 1) ? offsets[currentFrame+1] : stream.Length;

			while (stream.Position < end)
			{
				var type = stream.ReadASCII(4);
				var length = int2.Swap(stream.ReadUInt32());

				switch(type)
				{
					case "VQFR":
						DecodeVQFR(stream);
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
		public void DecodeVQFR(Stream s)
		{
			while (true)
			{
				// Chunks are aligned on even bytes; may be padded with a single null
				if (s.Peek() == 0) s.ReadByte();
				var type = s.ReadASCII(4);
				var subchunkLength = (int)int2.Swap(s.ReadUInt32());

				switch(type)
				{
					// Full frame-modifier
					case "CBFZ":
						Format80.DecodeInto(s.ReadBytes(subchunkLength), cbf);
					break;
					case "CBF0":
						cbf = s.ReadBytes(subchunkLength);
					break;

					// frame-modifier chunk
					case "CBP0":
					case "CBPZ":
						// Partial buffer is full; dump and recreate
						if (cbChunk == cbParts)
						{
							if (type == "CBP0")
								cbf = (byte[])cbp.Clone();
							else
								Format80.DecodeInto(cbp, cbf);

							cbOffset = cbChunk = 0;
						}

						var bytes = s.ReadBytes(subchunkLength);
						bytes.CopyTo(cbp,cbOffset);
						cbOffset += subchunkLength;
						cbChunk++;
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
						Format80.DecodeInto(s.ReadBytes(subchunkLength), origData);
						// This is the last subchunk
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
			for (var y = 0; y < blocks.Y; y++)
				for (var x = 0; x < blocks.X; x++)
				{
					var px = origData[x + y*blocks.X];
					var mod = origData[x + (y + blocks.Y)*blocks.X];
					for (var j = 0; j < blockHeight; j++)
						for (var i = 0; i < blockWidth; i++)
						{
							var cbfi = (mod*256 + px)*8 + j*blockWidth + i;
							var color = (mod == 0x0f) ? px : cbf[cbfi];
							frameData[y*blockHeight + j, x*blockWidth + i] = palette[color];
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
	}
}
