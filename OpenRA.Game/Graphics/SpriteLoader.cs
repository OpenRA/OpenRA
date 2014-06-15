#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Primitives;

namespace OpenRA.Graphics
{
	public class SpriteLoader
	{
		public readonly SheetBuilder SheetBuilder;
		readonly Cache<string, Sprite[]> sprites;
		readonly string[] exts;

		public SpriteLoader(string[] exts, SheetBuilder sheetBuilder)
		{
			SheetBuilder = sheetBuilder;

			// Include extension-less version
			this.exts = exts.Append("").ToArray();
			sprites = new Cache<string, Sprite[]>(CacheSpriteFrames);
		}

		Sprite[] CacheSpriteFrames(string filename)
		{
			using (var stream = GlobalFileSystem.OpenWithExts(filename, exts))
				return SpriteSource.LoadSpriteSource(stream, filename).Frames
					.Select(a => SheetBuilder.Add(a))
					.ToArray();
		}

		public Sprite[] LoadAllSprites(string filename) { return sprites[filename]; }
	}
}
