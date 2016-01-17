#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Support;

namespace OpenRA.Graphics
{
	class TheaterTemplate
	{
		public readonly Sprite[] Sprites;
		public readonly int Stride;
		public readonly int Variants;

		public TheaterTemplate(Sprite[] sprites, int stride, int variants)
		{
			Sprites = sprites;
			Stride = stride;
			Variants = variants;
		}
	}

	public sealed class Theater : IDisposable
	{
		readonly Dictionary<ushort, TheaterTemplate> templates = new Dictionary<ushort, TheaterTemplate>();
		readonly SheetBuilder sheetBuilder;
		readonly Sprite missingTile;
		readonly MersenneTwister random;
		TileSet tileset;

		public Theater(TileSet tileset)
		{
			this.tileset = tileset;
			var allocated = false;
			var type = tileset.EnableDepth ? SheetType.DualIndexed : SheetType.Indexed;

			Func<Sheet> allocate = () =>
			{
				if (allocated)
					throw new SheetOverflowException("Terrain sheet overflow. Try increasing the tileset SheetSize parameter.");
				allocated = true;

				return new Sheet(type, new Size(tileset.SheetSize, tileset.SheetSize));
			};

			sheetBuilder = new SheetBuilder(type, allocate);
			random = new MersenneTwister();

			var frameCache = new FrameCache(Game.ModData.SpriteLoaders);
			foreach (var t in tileset.Templates)
			{
				var variants = new List<Sprite[]>();

				foreach (var i in t.Value.Images)
				{
					var allFrames = frameCache[i];
					var frameCount = tileset.EnableDepth ? allFrames.Length / 2 : allFrames.Length;
					var indices = t.Value.Frames != null ? t.Value.Frames : Enumerable.Range(0, frameCount);
					variants.Add(indices.Select(j =>
					{
						var f = allFrames[j];
						var s = sheetBuilder.Allocate(f.Size, f.Offset);
						Util.FastCopyIntoChannel(s, 0, f.Data);

						if (tileset.EnableDepth)
							Util.FastCopyIntoChannel(s, 1, allFrames[j + frameCount].Data);

						return s;
					}).ToArray());
				}

				var allSprites = variants.SelectMany(s => s);

				// Ignore the offsets baked into R8 sprites
				if (tileset.IgnoreTileSpriteOffsets)
					allSprites = allSprites.Select(s => new Sprite(s.Sheet, s.Bounds, float2.Zero, s.Channel, s.BlendMode));

				templates.Add(t.Value.Id, new TheaterTemplate(allSprites.ToArray(), variants.First().Count(), t.Value.Images.Length));
			}

			// 1x1px transparent tile
			missingTile = sheetBuilder.Add(new byte[1], new Size(1, 1));

			Sheet.ReleaseBuffer();
		}

		public Sprite TileSprite(TerrainTile r, int? variant = null)
		{
			TheaterTemplate template;
			if (!templates.TryGetValue(r.Type, out template))
				return missingTile;

			if (r.Index >= template.Stride)
				return missingTile;

			var start = template.Variants > 1 ? variant.HasValue ? variant.Value : random.Next(template.Variants) : 0;
			return template.Sprites[start * template.Stride + r.Index];
		}

		public Rectangle TemplateBounds(TerrainTemplateInfo template, Size tileSize, MapGridType mapGrid)
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
					var u = mapGrid == MapGridType.Rectangular ? x : (x - y) / 2f;
					var v = mapGrid == MapGridType.Rectangular ? y : (x + y) / 2f;

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
