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
	public class TmpRAReader : ISpriteSource
	{
		public IReadOnlyList<ISpriteFrame> Frames { get; private set; }

		public TmpRAReader(Stream s)
		{
			var width = s.ReadUInt16();
			var height = s.ReadUInt16();
			var size = new Size(width, height);

			s.Position += 12;
			var imgStart = s.ReadUInt32();
			s.Position += 8;
			var indexEnd = s.ReadInt32();
			s.Position += 4;
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
