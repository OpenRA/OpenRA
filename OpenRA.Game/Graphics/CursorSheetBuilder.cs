#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
