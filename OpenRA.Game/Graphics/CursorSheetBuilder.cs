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

using System;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	static class CursorSheetBuilder
	{
		static Cache<string, Sprite[]> cursors = new Cache<string, Sprite[]>(LoadCursors);
		static readonly string[] exts = { ".shp" };

		static Sprite[] LoadCursors(string filename)
		{
			try
			{
				var shp = new Dune2ShpReader(FileSystem.OpenWithExts(filename, exts));
				return shp.Select(a => SheetBuilder.SharedInstance.Add(a.Image, a.Size)).ToArray();
			}
			catch (IndexOutOfRangeException) // This will occur when loading a custom (RA-format) .shp
			{
				var shp = new ShpReader(FileSystem.OpenWithExts(filename, exts));
				return shp.Select(a => SheetBuilder.SharedInstance.Add(a.Image, shp.Size)).ToArray();
			}
		}

		public static Sprite[] LoadAllSprites(string filename) { return cursors[filename]; }
	}
}
