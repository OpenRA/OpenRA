#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Drawing;
using System.IO;
using OpenRA.Graphics;

namespace OpenRA.FileFormats
{
	public class TmpTile : ISpriteFrame
	{
		public Size Size { get; private set; }
		public Size FrameSize { get; private set; }
		public float2 Offset { get { return float2.Zero; } }
		public byte[] Data { get; set; }
		public bool DisableExportPadding { get { return false; } }

		public TmpTile(byte[] data, Size size)
		{
			FrameSize = size;
			Data = data;

			if (data == null)
				Data = new byte[0];
			else
				Size = size;
		}
	}

	public class TmpTDReader : ISpriteSource
	{
		public IReadOnlyList<ISpriteFrame> Frames { get; private set; }

		public TmpTDReader(Stream s)
		{
			var width = s.ReadUInt16();
			var height = s.ReadUInt16();
			var size = new Size(width, height);

			s.Position += 8;
			var imgStart = s.ReadUInt32();
			s.Position += 8;
			var indexEnd = s.ReadInt32();
			var indexStart = s.ReadInt32();

			s.Position = indexStart;
			var count = indexEnd - indexStart;
			var tiles = new TmpTile[count];
			Frames = tiles.AsReadOnly();
			var tilesIndex = 0;
			foreach (var b in s.ReadBytes(count))
			{
				if (b != 255)
				{
					s.Position = imgStart + b * width * height;
					tiles[tilesIndex++] = new TmpTile(s.ReadBytes(width * height), size);
				}
				else
					tiles[tilesIndex++] = new TmpTile(null, size);
			}
		}
	}
}
