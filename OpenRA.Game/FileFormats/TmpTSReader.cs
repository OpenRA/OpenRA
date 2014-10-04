#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenRA.Graphics;

namespace OpenRA.FileFormats
{
	public class TmpTSTile : ISpriteFrame
	{
		public Size Size { get; private set; }
		public Size FrameSize { get { return Size; } }
		public float2 Offset { get { return float2.Zero; } }
		public byte[] Data { get; set; }

		public TmpTSTile(Stream s, Size size)
		{
			Size = size;

			// Ignore tile header for now
			s.Position += 52;

			Data = new byte[size.Width * size.Height];

			// Unpack tile data
			var width = 4;
			for (var i = 0; i < size.Height; i++)
			{
				var start = i * size.Width + (size.Width - width) / 2;
				for (var j = 0; j < width; j++)
					Data[start + j] = s.ReadUInt8();

				width += (i < size.Height / 2 - 1 ? 1 : -1) * 4;
			}

			// Ignore Z-data for now
			// Ignore extra data for now
		}
	}

	public class TmpTSReader : ISpriteSource
	{
		public IReadOnlyList<ISpriteFrame> Frames { get; private set; }

		public TmpTSReader(Stream s)
		{
			var templateWidth = s.ReadUInt32();
			var templateHeight = s.ReadUInt32();
			var tileWidth = s.ReadInt32();
			var tileHeight = s.ReadInt32();
			var size = new Size(tileWidth, tileHeight);
			var offsets = new uint[templateWidth * templateHeight];
			for (var i = 0; i < offsets.Length; i++)
				offsets[i] = s.ReadUInt32();

			var tiles = new List<TmpTSTile>();
			for (var i = 0; i < offsets.Length; i++)
			{
				s.Position = offsets[i];
				tiles.Add(new TmpTSTile(s, size));
			}

			Frames = tiles.ToArray().AsReadOnly();
		}
	}
}
