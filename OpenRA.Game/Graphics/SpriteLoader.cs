#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.IO;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public class SpriteLoader
	{
		public SpriteLoader(string[] exts, SheetBuilder sheetBuilder)
		{
			SheetBuilder = sheetBuilder;
			this.exts = exts;
			sprites = new Cache<string, Sprite[]>(LoadSprites);
		}

		readonly SheetBuilder SheetBuilder;
		readonly Cache<string, Sprite[]> sprites;
		readonly string[] exts;

		Sprite[] LoadSprites(string filename)
		{
			BinaryReader reader = new BinaryReader(FileSystem.OpenWithExts(filename, exts));

			var ImageCount = reader.ReadUInt16();
			if (ImageCount == 0)
			{
				var shp = new ShpTSReader(FileSystem.OpenWithExts(filename, exts));
				return shp.Select(a => SheetBuilder.Add(a.Image, shp.Size, true)).ToArray();
			}
			else
			{
				var shp = new ShpReader(FileSystem.OpenWithExts(filename, exts));
				return shp.Frames.Select(a => SheetBuilder.Add(a.Image, shp.Size, true)).ToArray();
			}
		}

		public Sprite[] LoadAllSprites(string filename) { return sprites[filename]; }
	}
}
