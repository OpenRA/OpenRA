#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.Video;

namespace OpenRA.Mods.Cnc.FileFormats
{
	public class WsaReader : IVideo
	{
		public ushort FrameCount { get; }
		public byte Framerate => 1;
		public ushort Width { get; }
		public ushort Height { get; }

		public int CurrentFrameNumber { get; private set; }
		public uint[,] CurrentFrameData
		{
			get
			{
				if (cachedFrameNumber != CurrentFrameNumber)
					LoadFrame();

				return cachedCurrentFrameData;
			}
		}

		public bool HasAudio => false;
		public byte[] AudioData => null;
		public int AudioChannels => 0;
		public int SampleBits => 0;
		public int SampleRate => 0;

		readonly Stream stream;
		readonly uint[] palette;
		readonly uint[] frameOffsets;

		byte[] previousFramePaletteIndexData;
		byte[] currentFramePaletteIndexData;

		int cachedFrameNumber = -1;
		uint[,] cachedCurrentFrameData;

		public WsaReader(Stream stream)
		{
			this.stream = stream;

			FrameCount = stream.ReadUInt16();

			/*var x = */stream.ReadUInt16();
			/*var y = */stream.ReadUInt16();

			Width = stream.ReadUInt16();
			Height = stream.ReadUInt16();

			var delta = stream.ReadUInt16() + 37;
			var flags = stream.ReadUInt16();

			frameOffsets = new uint[FrameCount + 2];
			for (var i = 0; i < frameOffsets.Length; i++)
				frameOffsets[i] = stream.ReadUInt32();

			if (flags == 1)
			{
				palette = new uint[256];
				for (var i = 0; i < palette.Length; i++)
				{
					var r = (byte)(stream.ReadByte() << 2);
					var g = (byte)(stream.ReadByte() << 2);
					var b = (byte)(stream.ReadByte() << 2);

					// Replicate high bits into the (currently zero) low bits.
					r |= (byte)(r >> 6);
					g |= (byte)(g >> 6);
					b |= (byte)(b >> 6);

					palette[i] = (uint)((255 << 24) | (r << 16) | (g << 8) | b);
				}

				for (var i = 0; i < frameOffsets.Length; i++)
					frameOffsets[i] += 768;
			}

			Reset();
		}

		public void Reset()
		{
			CurrentFrameNumber = 0;
			previousFramePaletteIndexData = null;
			LoadFrame();
		}

		public void AdvanceFrame()
		{
			previousFramePaletteIndexData = currentFramePaletteIndexData;
			CurrentFrameNumber++;
			LoadFrame();
		}

		void LoadFrame()
		{
			if (CurrentFrameNumber >= FrameCount)
				return;

			stream.Seek(frameOffsets[CurrentFrameNumber], SeekOrigin.Begin);

			var dataLength = frameOffsets[CurrentFrameNumber + 1] - frameOffsets[CurrentFrameNumber];

			var rawData = StreamExts.ReadBytes(stream, (int)dataLength);
			var intermediateData = new byte[Width * Height];

			// Format80 decompression
			LCWCompression.DecodeInto(rawData, intermediateData);

			// and Format40 decompression
			currentFramePaletteIndexData = new byte[Width * Height];
			if (previousFramePaletteIndexData == null)
				Array.Clear(currentFramePaletteIndexData, 0, currentFramePaletteIndexData.Length);
			else
				Array.Copy(previousFramePaletteIndexData, currentFramePaletteIndexData, currentFramePaletteIndexData.Length);

			XORDeltaCompression.DecodeInto(intermediateData, currentFramePaletteIndexData, 0);

			var c = 0;
			var frameSize = Exts.NextPowerOf2(Math.Max(Width, Height));
			cachedCurrentFrameData = new uint[frameSize, frameSize];
			for (var y = 0; y < Height; y++)
				for (var x = 0; x < Width; x++)
					cachedCurrentFrameData[y, x] = palette[currentFramePaletteIndexData[c++]];
		}
	}
}
