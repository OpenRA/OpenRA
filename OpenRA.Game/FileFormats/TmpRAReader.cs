#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.FileFormats
{
	public class TmpRAReader : ISpriteSource
	{
		readonly List<ISpriteFrame> tiles = new List<ISpriteFrame>();
		public IEnumerable<ISpriteFrame> Frames { get { return tiles; } }
		public bool CacheWhenLoadingTileset { get { return false; } }

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
			foreach (byte b in s.ReadBytes(indexEnd - indexStart))
			{
				if (b != 255)
				{
					s.Position = imgStart + b * width * height;
					tiles.Add(new TmpTile(s.ReadBytes(width * height), size));
				}
				else
					tiles.Add(new TmpTile(null, size));
			}
		}
	}
}
