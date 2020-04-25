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
		readonly Stream stream;

		public ushort Frames { get { return frameCount; } }
		public byte Framerate { get { return 1; } }
		public ushort Width { get { return width; } }
		public ushort Height { get { return height; } }

		readonly ushort frameCount;
		readonly ushort width;
		readonly ushort height;
		readonly uint[] palette;
		readonly uint[] frameOffsets;

		uint[,] coloredFrameData;
		public uint[,] FrameData { get { return coloredFrameData; } }

		int currentFrame;
		byte[] previousFrameData;
		byte[] currentFrameData;

		public byte[] AudioData { get { return null; } }
		public int CurrentFrame { get { return currentFrame; } }
		public int SampleRate { get { return 0; } }
		public int SampleBits { get { return 0; } }
		public int AudioChannels { get { return 0; } }
		public bool HasAudio { get { return false; } }

		public WsaReader(Stream stream)
		{
			this.stream = stream;

			frameCount = stream.ReadUInt16();

			var x = stream.ReadUInt16();
			var y = stream.ReadUInt16();

			width = stream.ReadUInt16();
			height = stream.ReadUInt16();

			var delta = stream.ReadUInt16() + 37;
			var flags = stream.ReadUInt16();

			frameOffsets = new uint[frameCount + 2];
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
			currentFrame = 0;
			previousFrameData = null;
			LoadFrame();
		}

		public void AdvanceFrame()
		{
			previousFrameData = currentFrameData;
			currentFrame++;
			LoadFrame();
		}

		void LoadFrame()
		{
			if (currentFrame >= frameCount)
				return;

			stream.Seek(frameOffsets[currentFrame], SeekOrigin.Begin);

			var dataLength = frameOffsets[currentFrame + 1] - frameOffsets[currentFrame];

			var rawData = StreamExts.ReadBytes(stream, (int)dataLength);
			var intermediateData = new byte[width * height];

			// Format80 decompression
			LCWCompression.DecodeInto(rawData, intermediateData);

			// and Format40 decompression
			currentFrameData = new byte[width * height];
			if (previousFrameData == null)
				Array.Clear(currentFrameData, 0, currentFrameData.Length);
			else
				Array.Copy(previousFrameData, currentFrameData, currentFrameData.Length);

			XORDeltaCompression.DecodeInto(intermediateData, currentFrameData, 0);

			var c = 0;
			var frameSize = Exts.NextPowerOf2(Math.Max(width, height));
			coloredFrameData = new uint[frameSize, frameSize];
			for (var y = 0; y < height; y++)
				for (var x = 0; x < width; x++)
					coloredFrameData[y, x] = palette[currentFrameData[c++]];
		}
	}
}
