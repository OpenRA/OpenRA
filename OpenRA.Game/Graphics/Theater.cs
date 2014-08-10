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
	public sealed class Theater : IDisposable
	{
		readonly Dictionary<ushort, Sprite[]> templates = new Dictionary<ushort, Sprite[]>();
		readonly SheetBuilder sheetBuilder;
		readonly Sprite missingTile;

		Sprite[] LoadTemplate(string filename, string[] exts, Dictionary<string, ISpriteSource> sourceCache, int[] frames)
		{
			ISpriteSource source;
			if (!sourceCache.ContainsKey(filename))
			{
				using (var s = GlobalFileSystem.OpenWithExts(filename, exts))
					source = SpriteSource.LoadSpriteSource(s, filename);

				if (source.CacheWhenLoadingTileset)
					sourceCache.Add(filename, source);
			}
			else
				source = sourceCache[filename];

			if (frames != null)
			{
				var ret = new List<Sprite>();
				var srcFrames = source.Frames.ToArray();
				foreach (var i in frames)
					ret.Add(sheetBuilder.Add(srcFrames[i]));

				return ret.ToArray();
			}

			return source.Frames.Select(f => sheetBuilder.Add(f)).ToArray();
		}

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

			var sourceCache = new Dictionary<string, ISpriteSource>();
			sheetBuilder = new SheetBuilder(SheetType.Indexed, allocate);
			foreach (var t in tileset.Templates)
				templates.Add(t.Value.Id, LoadTemplate(t.Value.Image, tileset.Extensions, sourceCache, t.Value.Frames));

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

		public void Dispose()
		{
			sheetBuilder.Dispose();
		}
	}
}
