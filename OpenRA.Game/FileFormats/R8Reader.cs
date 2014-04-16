#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.FileFormats
{
	class R8Image : ISpriteFrame
	{
		public Size Size { get; private set; }
		public Size FrameSize { get; private set; }
		public float2 Offset { get; private set; }
		public byte[] Data { get; set; }

		public R8Image(Stream s)
		{
			// Scan forward until we find some data
			var type = s.ReadUInt8();
			while (type == 0)
				type = s.ReadUInt8();

			var width = s.ReadInt32();
			var height = s.ReadInt32();
			var x = s.ReadInt32();
			var y = s.ReadInt32();

			Size = new Size(width, height);
			Offset = new int2(width/2 - x, height/2 - y);

			/*var imageOffset = */s.ReadInt32();
			var paletteOffset = s.ReadInt32();
			var bpp = s.ReadUInt8();
			if (bpp != 8)
				throw new InvalidDataException("Error: {0} bits per pixel are not supported.".F(bpp));

			var frameHeight = s.ReadUInt8();
			var frameWidth = s.ReadUInt8();
			FrameSize = new Size(frameWidth, frameHeight);

			// Skip alignment byte
			s.ReadUInt8();

			Data = s.ReadBytes(width*height);

			// Ignore palette
			if (type == 1 && paletteOffset != 0)
				s.Seek(520, SeekOrigin.Current);
		}
	}

	public class R8Reader : ISpriteSource
	{
		readonly List<R8Image> frames = new List<R8Image>();
		public IEnumerable<ISpriteFrame> Frames { get { return frames.Cast<ISpriteFrame>(); } }
		public bool CacheWhenLoadingTileset { get { return true; } }

		public readonly int ImageCount;
		public R8Reader(Stream stream)
		{
			while (stream.Position < stream.Length)
			{
				frames.Add(new R8Image(stream));
				ImageCount++;
			}
		}
	}
}
