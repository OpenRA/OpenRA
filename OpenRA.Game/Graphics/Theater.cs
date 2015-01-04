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
		TileSet tileset;

		public Theater(TileSet tileset)
		{
			this.tileset = tileset;
			var allocated = false;
			Func<Sheet> allocate = () =>
			{
				if (allocated)
					throw new SheetOverflowException("Terrain sheet overflow. Try increasing the tileset SheetSize parameter.");
				allocated = true;

				return new Sheet(new Size(tileset.SheetSize, tileset.SheetSize));
			};

			sheetBuilder = new SheetBuilder(SheetType.Indexed, allocate);
			templates = new Dictionary<ushort, Sprite[]>();

			var frameCache = new FrameCache(Game.ModData.SpriteLoaders, tileset.Extensions);
			foreach (var t in tileset.Templates)
			{
				var allFrames = frameCache[t.Value.Image];
				var frames = t.Value.Frames != null ? t.Value.Frames.Select(f => allFrames[f]).ToArray() : allFrames;
				var sprites = frames.Select(f => sheetBuilder.Add(f));

				// Ignore the offsets baked into R8 sprites
				if (tileset.IgnoreTileSpriteOffsets)
					sprites = sprites.Select(s => new Sprite(s.Sheet, s.Bounds, float2.Zero, s.Channel, s.BlendMode));

				templates.Add(t.Value.Id, sprites.ToArray());
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

		public Rectangle TemplateBounds(TerrainTemplateInfo template, Size tileSize, TileShape tileShape)
		{
			Rectangle? templateRect = null;

			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					var tile = new TerrainTile(template.Id, (byte)(i++));
					var tileInfo = tileset.GetTileInfo(tile);

					// Empty tile
					if (tileInfo == null)
						continue;

					var sprite = TileSprite(tile);
					var u = tileShape == TileShape.Rectangle ? x : (x - y) / 2f;
					var v = tileShape == TileShape.Rectangle ? y : (x + y) / 2f;

					var tl = new float2(u * tileSize.Width, (v - 0.5f * tileInfo.Height) * tileSize.Height) - 0.5f * sprite.Size;
					var rect = new Rectangle((int)(tl.X + sprite.Offset.X), (int)(tl.Y + sprite.Offset.Y), (int)sprite.Size.X, (int)sprite.Size.Y);
					templateRect = templateRect.HasValue ? Rectangle.Union(templateRect.Value, rect) : rect;
				}
			}

			return templateRect.HasValue ? templateRect.Value : Rectangle.Empty;
		}

		public Sheet Sheet { get { return sheetBuilder.Current; } }

		public void Dispose()
		{
			sheetBuilder.Dispose();
		}
	}
}
