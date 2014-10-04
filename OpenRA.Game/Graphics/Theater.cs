#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Graphics
{
	public class Theater
	{
		SheetBuilder sheetBuilder;
		Dictionary<ushort, Sprite[]> templates;
		Sprite missingTile;

		public Theater(TileSet tileset)
		{
			var allocated = false;
			Func<Sheet> allocate = () =>
			{
				if (allocated)
					throw new SheetOverflowException("Terrain sheet overflow. Try increasing the tileset SheetSize parameter.");
				allocated = true;

				return new Sheet(new Size(tileset.SheetSize, tileset.SheetSize), true);
			};

			sheetBuilder = new SheetBuilder(SheetType.Indexed, allocate);
			templates = new Dictionary<ushort, Sprite[]>();

			// We manage the SheetBuilder ourselves, to avoid loading all of the tileset images
			var spriteLoader = new SpriteLoader(tileset.Extensions, null);
			foreach (var t in tileset.Templates)
			{
				var allFrames = spriteLoader.LoadAllFrames(t.Value.Image);
				var frames = t.Value.Frames != null ? t.Value.Frames.Select(f => allFrames[f]).ToArray() : allFrames;
				templates.Add(t.Value.Id, frames.Select(f => sheetBuilder.Add(f)).ToArray());
			}

			// 1x1px transparent tile
			missingTile = sheetBuilder.Add(new byte[1], new Size(1, 1));

			Sheet.ReleaseBuffer();
		}

		public Sprite TileSprite(TerrainTile r)
		{
			Sprite[] template;
			if (!templates.TryGetValue(r.Type, out template))
				return missingTile;

			if (r.Index >= template.Length)
				return missingTile;

			return template[r.Index];
		}

		public Sheet Sheet { get { return sheetBuilder.Current; } }
	}
}
