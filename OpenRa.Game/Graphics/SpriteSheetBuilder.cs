#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	static class SpriteSheetBuilder
	{
		public static void Initialize( Map map )
		{
			exts = new[] {
				"." + map.Theater.Substring( 0, 3 ).ToLowerInvariant(),
				".shp",
				".tem",
				".sno",
				".int" };
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
