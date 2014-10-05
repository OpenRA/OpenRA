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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.SpriteLoaders
{
	public class ShpTSLoader : ISpriteLoader
	{
		class ShpTSFrame : ISpriteFrame
		{
			public Size Size { get; private set; }
			public Size FrameSize { get; private set; }
			public float2 Offset { get; private set; }
			public byte[] Data { get; set; }
			public bool DisableExportPadding { get { return false; } }

			public readonly uint FileOffset;
			public readonly byte Format;

			public ShpTSFrame(Stream stream, Size frameSize)
			{
				var x = stream.ReadUInt16();
				var y = stream.ReadUInt16();
				var width = stream.ReadUInt16();
				var height = stream.ReadUInt16();

				// Note: the mixed Integer / fp division is intentional, and required for calculating the correct offset.
				Offset = new float2(x + width / 2 - 0.5f * frameSize.Width, y + height / 2 - 0.5f * frameSize.Height);
				Size = new Size(width, height);
				FrameSize = frameSize;

				Format = stream.ReadUInt8();
				stream.Position += 11;
				FileOffset = stream.ReadUInt32();
			}
		}

		bool IsShpTS(Stream s)
		{
			var start = s.Position;

			// First word is zero
			if (s.ReadUInt16() != 0)
			{
				s.Position = start;
				return false;
			}

			// Sanity Check the image count
			s.Position += 4;
			var imageCount = s.ReadUInt16();
			if (s.Position + 24 * imageCount > s.Length)
			{
				s.Position = start;
				return false;
			}

			// Check the size and format flag
			// Some files define bogus frames, so loop until we find a valid one
			s.Position += 4;
			ushort w, h, f = 0;
			byte type;
			do
			{
				w = s.ReadUInt16();
				h = s.ReadUInt16();
				type = s.ReadUInt8();
			}
			while (w == 0 && h == 0 && f++ < imageCount);

			s.Position = start;
			return type < 4;
		}

		ShpTSFrame[] ParseFrames(Stream s)
		{
			var start = s.Position;

			s.ReadUInt16();
			var width = s.ReadUInt16();
			var height = s.ReadUInt16();
			var size = new Size(width, height);
			var frameCount = s.ReadUInt16();

			var frames = new ShpTSFrame[frameCount];
			for (var i = 0; i < frames.Length; i++)
				frames[i] = new ShpTSFrame(s, size);

			for (var i = 0; i < frameCount; i++)
			{
				var f = frames[i];
				if (f.FileOffset == 0)
					continue;

				s.Position = f.FileOffset;

				var frameSize = f.Size.Width * f.Size.Height;

				// Uncompressed
				if (f.Format == 1 || f.Format == 0)
					f.Data = s.ReadBytes(frameSize);

				// Uncompressed scanlines
				else if (f.Format == 2)
				{
					f.Data = new byte[frameSize];
					for (var j = 0; j < f.Size.Height; j++)
					{
						var length = s.ReadUInt16() - 2;
						var offset = f.Size.Width * j;
						s.ReadBytes(f.Data, offset, length);
					}
				}

				// RLE-zero compressed scanlines
				else if (f.Format == 3)
				{
					f.Data = new byte[frameSize];
					for (var j = 0; j < f.Size.Height; j++)
					{
						var length = s.ReadUInt16() - 2;
						var offset = f.Size.Width * j;
						Format2.DecodeInto(s.ReadBytes(length), f.Data, offset);
					}
				}
			}

			s.Position = start;
			return frames;
		}

		public bool TryParseSprite(Stream s, out ISpriteFrame[] frames)
		{
			if (!IsShpTS(s))
			{
				frames = null;
				return false;
			}

			frames = ParseFrames(s);
			return true;
		}
	}
}
