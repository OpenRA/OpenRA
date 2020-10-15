#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class TerrainRendererInfo : TraitInfo, ITiledTerrainRendererInfo
	{
		bool ITiledTerrainRendererInfo.ValidateTileSprites(ITemplatedTerrainInfo terrainInfo, Action<string> onError)
		{
			var missingImages = new HashSet<string>();
			var failed = false;
			Action<uint, string> onMissingImage = (id, f) =>
			{
				onError("\tTemplate `{0}` references sprite `{1}` that does not exist.".F(id, f));
				missingImages.Add(f);
				failed = true;
			};

			var tileCache = new Theater((TileSet)terrainInfo, onMissingImage);
			foreach (var t in terrainInfo.Templates)
			{
				for (var v = 0; v < t.Value.Images.Length; v++)
				{
					if (!missingImages.Contains(t.Value.Images[v]))
					{
						for (var i = 0; i < t.Value.TilesCount; i++)
						{
							if (t.Value[i] == null || tileCache.HasTileSprite(new TerrainTile(t.Key, (byte)i), v))
								continue;

							onError("\tTemplate `{0}` references frame {1} that does not exist in sprite `{2}`.".F(t.Key, i, t.Value.Images[v]));
							failed = true;
						}
					}
				}
			}

			return failed;
		}

		public override object Create(ActorInitializer init) { return new TerrainRenderer(init.World); }
	}

	public sealed class TerrainRenderer : IRenderTerrain, IWorldLoaded, INotifyActorDisposing, ITiledTerrainRenderer
	{
		readonly Map map;
		readonly Dictionary<string, TerrainSpriteLayer> spriteLayers = new Dictionary<string, TerrainSpriteLayer>();
		readonly TileSet terrainInfo;
		readonly Theater tileCache;
		bool disposed;

		public TerrainRenderer(World world)
		{
			map = world.Map;
			terrainInfo = map.Rules.TerrainInfo as TileSet;
			if (terrainInfo == null)
				throw new InvalidDataException("TerrainRenderer can only be used with the default TileSet");

			tileCache = new Theater(terrainInfo);
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			foreach (var template in terrainInfo.Templates)
			{
				var palette = template.Value.Palette ?? TileSet.TerrainPaletteInternalName;
				spriteLayers.GetOrAdd(palette, pal =>
					new TerrainSpriteLayer(world, wr, tileCache.Sheet, BlendMode.Alpha, wr.Palette(palette), world.Type != WorldType.Editor));
			}

			foreach (var cell in map.AllCells)
				UpdateCell(cell);

			map.Tiles.CellEntryChanged += UpdateCell;
			map.Height.CellEntryChanged += UpdateCell;
		}

		public void UpdateCell(CPos cell)
		{
			var tile = map.Tiles[cell];
			var palette = TileSet.TerrainPaletteInternalName;
			if (terrainInfo.Templates.TryGetValue(tile.Type, out var template))
				palette = template.Palette ?? palette;

			foreach (var kv in spriteLayers)
				kv.Value.Update(cell, palette == kv.Key ? tileCache.TileSprite(tile) : null, false);
		}

		void IRenderTerrain.RenderTerrain(WorldRenderer wr, Viewport viewport)
		{
			foreach (var kv in spriteLayers.Values)
				kv.Draw(wr.Viewport);

			foreach (var r in wr.World.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render(wr);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			map.Tiles.CellEntryChanged -= UpdateCell;
			map.Height.CellEntryChanged -= UpdateCell;

			foreach (var kv in spriteLayers.Values)
				kv.Dispose();

			tileCache.Dispose();
			disposed = true;
		}

		Sheet ITiledTerrainRenderer.Sheet { get { return tileCache.Sheet; } }

		Sprite ITiledTerrainRenderer.TileSprite(TerrainTile r, int? variant)
		{
			return tileCache.TileSprite(r, variant);
		}

		Rectangle ITiledTerrainRenderer.TemplateBounds(TerrainTemplateInfo template)
		{
			Rectangle? templateRect = null;
			var tileSize = map.Grid.TileSize;

			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					var tile = new TerrainTile(template.Id, (byte)(i++));
					if (!terrainInfo.TryGetTileInfo(tile, out var tileInfo))
						continue;

					var sprite = tileCache.TileSprite(tile);
					var u = map.Grid.Type == MapGridType.Rectangular ? x : (x - y) / 2f;
					var v = map.Grid.Type == MapGridType.Rectangular ? y : (x + y) / 2f;

					var tl = new float2(u * tileSize.Width, (v - 0.5f * tileInfo.Height) * tileSize.Height) - 0.5f * sprite.Size;
					var rect = new Rectangle((int)(tl.X + sprite.Offset.X), (int)(tl.Y + sprite.Offset.Y), (int)sprite.Size.X, (int)sprite.Size.Y);
					templateRect = templateRect.HasValue ? Rectangle.Union(templateRect.Value, rect) : rect;
				}
			}

			return templateRect ?? Rectangle.Empty;
		}

		IEnumerable<IRenderable> ITiledTerrainRenderer.RenderUIPreview(WorldRenderer wr, TerrainTemplateInfo template, int2 origin, float scale)
		{
			if (template == null)
				yield break;

			var ts = map.Grid.TileSize;
			var gridType = map.Grid.Type;

			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					var tile = new TerrainTile(template.Id, (byte)i++);
					if (!terrainInfo.TryGetTileInfo(tile, out var tileInfo))
						continue;

					var sprite = tileCache.TileSprite(tile, 0);
					var u = gridType == MapGridType.Rectangular ? x : (x - y) / 2f;
					var v = gridType == MapGridType.Rectangular ? y : (x + y) / 2f;
					var offset = (new float2(u * ts.Width, (v - 0.5f * tileInfo.Height) * ts.Height) - 0.5f * sprite.Size.XY).ToInt2();
					var palette = template.Palette ?? TileSet.TerrainPaletteInternalName;

					yield return new UISpriteRenderable(sprite, WPos.Zero, origin + offset, 0, wr.Palette(palette), scale);
				}
			}
		}

		IEnumerable<IRenderable> ITiledTerrainRenderer.RenderPreview(WorldRenderer wr, TerrainTemplateInfo template, WPos origin)
		{
			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					var tile = new TerrainTile(template.Id, (byte)i++);
					if (!terrainInfo.TryGetTileInfo(tile, out var tileInfo))
						continue;

					var sprite = tileCache.TileSprite(tile, 0);
					var offset = map.Offset(new CVec(x, y), tileInfo.Height);
					var palette = wr.Palette(template.Palette ?? TileSet.TerrainPaletteInternalName);

					yield return new SpriteRenderable(sprite, origin, offset, 0, palette, 1, false);
				}
			}
		}
	}
}
