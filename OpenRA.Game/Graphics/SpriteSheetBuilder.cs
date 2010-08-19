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
	public static class SpriteSheetBuilder
	{
		public static void Initialize( TileSet tileset )
		{
			exts = tileset.Extensions;
			sprites = new Cache<string, Sprite[]>( LoadSprites );
		}

		static Cache<string, Sprite[]> sprites;
		static string[] exts;

		static Sprite[] LoadSprites(string filename)
		{
			var shp = new ShpReader(FileSystem.OpenWithExts(filename, exts));
			return shp.Select(a => SheetBuilder.SharedInstance.Add(a.Image, shp.Size)).ToArray();
		}

		public static Sprite[] LoadAllSprites(string filename) { return sprites[filename]; }
	}
}
