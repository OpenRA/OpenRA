#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public static class SpriteLoader
	{
		public static void Initialize( TileSet tileset, SheetBuilder sheetBuilder )
		{
			exts = tileset.Extensions;
            SheetBuilder = sheetBuilder;
			sprites = new Cache<string, Sprite[]>( LoadSprites );
		}

        static SheetBuilder SheetBuilder;
		static Cache<string, Sprite[]> sprites;
		static string[] exts;

		static Sprite[] LoadSprites(string filename)
		{
			var shp = new ShpReader(FileSystem.OpenWithExts(filename, exts));
			return shp.Select(a => SheetBuilder.Add(a.Image, shp.Size)).ToArray();
		}

		public static Sprite[] LoadAllSprites(string filename) { return sprites[filename]; }
	}
}
