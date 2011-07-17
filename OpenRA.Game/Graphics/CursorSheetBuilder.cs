#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Graphics
{
	public class CursorSheetBuilder
	{
		ModData modData;
		Cache<string, Sprite[]> cursors;
		readonly string[] exts = { ".shp" };

		public CursorSheetBuilder( ModData modData )
		{
			this.modData = modData;
			this.cursors = new Cache<string, Sprite[]>( LoadCursors );
		}

		Sprite[] LoadCursors(string filename)
        {
			var shp = new ShpReader(FileSystem.OpenWithExts(filename, exts));
			return shp.Select(a => modData.SheetBuilder.Add(a.Image, shp.Size)).ToArray();
		}

		public Sprite[] LoadAllSprites(string filename) { return cursors[filename]; }
	}
}
