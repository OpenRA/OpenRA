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
using OpenRA.Video;

namespace OpenRA.Mods.Cnc.FileFormats
{
	public class WsaVideo : IVideo
	{
		public ushort FrameCount { get; }
		public byte Framerate => 1;
		public ushort Width { get; }
		public ushort Height { get; }

		public byte[] CurrentFrameData { get; }
		public int CurrentFrameIndex { get; private set; }

		public bool HasAudio => false;
		public byte[] AudioData => null;
		public int AudioChannels => 0;
		public int SampleBits => 0;
		public int SampleRate => 0;

		readonly Stream stream;
		readonly byte[] paletteBytes;
		readonly uint[] frameOffsets;
		readonly ushort totalFrameWidth;

		byte[] previousFramePaletteIndexData;
		byte[] currentFramePaletteIndexData;

		public WsaVideo(Stream stream, bool useFramePadding)
		{
			this.stream = stream;

			FrameCount = stream.ReadUInt16();

			/*var x = */stream.ReadUInt16();
			/*var y = */stream.ReadUInt16();

			Width = stream.ReadUInt16();
			Height = stream.ReadUInt16();

			/*var delta = */stream.ReadUInt16(); /* + 37*/
			var flags = stream.ReadUInt16();

			frameOffsets = new uint[FrameCount + 2];
			for (var i = 0; i < frameOffsets.Length; i++)
				frameOffsets[i] = stream.ReadUInt32();

			if (flags == 1)
			{
				paletteBytes = new byte[1024];
				for (var i = 0; i < paletteBytes.Length;)
				{
					var r = (byte)(stream.ReadByte() << 2);
					var g = (byte)(stream.ReadByte() << 2);
					var b = (byte)(stream.ReadByte() << 2);

					// Replicate high bits into the (currently zero) low bits.
					r |= (byte)(r >> 6);
					g |= (byte)(g >> 6);
					b |= (byte)(b >> 6);

					paletteBytes[i++] = b;
					paletteBytes[i++] = g;
					paletteBytes[i++] = r;
					paletteBytes[i++] = 255;
				}

				for (var i = 0; i < frameOffsets.Length; i++)
					frameOffsets[i] += 768;
			}

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
			CurrentFrameIndex = 0;
			previousFramePaletteIndexData = null;
			LoadFrame();
		}

		public void AdvanceFrame()
		{
			previousFramePaletteIndexData = currentFramePaletteIndexData;
			CurrentFrameIndex++;
			LoadFrame();
		}

		void LoadFrame()
		{
			if (CurrentFrameIndex >= FrameCount)
				return;

			stream.Seek(frameOffsets[CurrentFrameIndex], SeekOrigin.Begin);

			var dataLength = frameOffsets[CurrentFrameIndex + 1] - frameOffsets[CurrentFrameIndex];

			var rawData = stream.ReadBytes((int)dataLength);
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
			var position = 0;
			for (var y = 0; y < Height; y++)
			{
				for (var x = 0; x < Width; x++)
				{
					var colorIndex = currentFramePaletteIndexData[c++];
					CurrentFrameData[position++] = paletteBytes[colorIndex * 4];
					CurrentFrameData[position++] = paletteBytes[colorIndex * 4 + 1];
					CurrentFrameData[position++] = paletteBytes[colorIndex * 4 + 2];
					CurrentFrameData[position++] = paletteBytes[colorIndex * 4 + 3];
				}

				// Recalculate the position in the byte array to the start of the next pixel row just in case there is padding in the frame.
				position = (y + 1) * totalFrameWidth * 4;
			}
		}
	}
}
