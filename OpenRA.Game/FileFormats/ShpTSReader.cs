#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Graphics;

namespace OpenRA.FileFormats
{
	class FrameHeader : ISpriteFrame
	{
		public Size Size { get; private set; }
		public Size FrameSize { get; private set; }
		public float2 Offset { get; private set; }
		public byte[] Data { get; set; }

		public readonly uint FileOffset;
		public readonly byte Format;

		public FrameHeader(Stream stream, Size frameSize)
		{
			var x = stream.ReadUInt16();
			var y = stream.ReadUInt16();
			var width = stream.ReadUInt16();
			var height = stream.ReadUInt16();

			Offset = new float2(x + 0.5f * (width - frameSize.Width), y + 0.5f * (height - frameSize.Height));
			Size = new Size(width, height);
			FrameSize = frameSize;

			Format = stream.ReadUInt8();
			stream.Position += 11;
			FileOffset = stream.ReadUInt32();
		}
	}

	public class ShpTSReader : ISpriteSource
	{
		readonly List<FrameHeader> frames = new List<FrameHeader>();
		Lazy<IEnumerable<ISpriteFrame>> spriteFrames;
		public IEnumerable<ISpriteFrame> Frames { get { return spriteFrames.Value; } }
		public bool CacheWhenLoadingTileset { get { return false; } }

		public ShpTSReader(Stream stream)
		{
			stream.ReadUInt16();
			var width = stream.ReadUInt16();
			var height = stream.ReadUInt16();
			var size = new Size(width, height);
			var frameCount = stream.ReadUInt16();

			for (var i = 0; i < frameCount; i++)
				frames.Add(new FrameHeader(stream, size));

			for (var i = 0; i < frameCount; i++)
			{
				var f = frames[i];
				if (f.FileOffset == 0)
					continue;

				stream.Position = f.FileOffset;

				// Uncompressed
				if (f.Format == 1 || f.Format == 0)
					f.Data = stream.ReadBytes(f.Size.Width * f.Size.Height);

				// Uncompressed scanlines
				else if (f.Format == 2)
				{
					f.Data = new byte[f.Size.Width * f.Size.Height];
					for (var j = 0; j < f.Size.Height; j++)
					{
						var length = stream.ReadUInt16() - 2;
						stream.Read(f.Data, f.Size.Width * j, length);
					}
				}

				// RLE-zero compressed scanlines
				else if (f.Format == 3)
				{
					f.Data = new byte[f.Size.Width * f.Size.Height];

					for (var j = 0; j < f.Size.Height; j++)
					{
						var length = stream.ReadUInt16() - 2;
						Format2.DecodeInto(stream.ReadBytes(length), f.Data, j * f.Size.Width);
					}
				}
			}

			spriteFrames = Exts.Lazy(() => frames.Cast<ISpriteFrame>());
		}
	}
}