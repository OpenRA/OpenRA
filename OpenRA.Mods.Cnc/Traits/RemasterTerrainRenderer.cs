#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using System.Numerics;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Terrain;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	public class RemasterTerrainRendererInfo : TraitInfo, ITiledTerrainRendererInfo
	{
		public override object Create(ActorInitializer init) { return new RemasterTerrainRenderer(init.World); }

		bool ITiledTerrainRendererInfo.ValidateTileSprites(ITemplatedTerrainInfo terrainInfo, Action<string> onError)
		{
			var failed = false;
			var tileCache = new RemasterTileCache((RemasterTerrain)terrainInfo);
			foreach (var t in terrainInfo.Templates)
			{
				var templateInfo = (RemasterTerrainTemplateInfo)t.Value;
				foreach (var kv in templateInfo.RemasteredFilenames)
				{
					for (var i = 0; i < kv.Value.Length; i++)
					{
						if (!tileCache.HasTileSprite(new TerrainTile(t.Key, (byte)kv.Key), i))
						{
							onError($"\tTemplate `{t.Key}` tile {kv.Key} references sprite `{templateInfo.RemasteredFilenames[i]}` that does not exist.");
							failed = true;
						}
					}
				}
			}

			return failed;
		}
	}

	public sealed class RemasterTerrainRenderer : IRenderTerrain, IWorldLoaded, INotifyActorDisposing, ITiledTerrainRenderer, ITick
	{
		readonly Map map;
		readonly RemasterTerrain terrainInfo;
		readonly RemasterTileCache tileCache;
		TerrainSpriteLayer[] spriteLayers;
		WorldRenderer worldRenderer;
		int frame;
		bool disposed;

		public RemasterTerrainRenderer(World world)
		{
			map = world.Map;

			terrainInfo = map.Rules.TerrainInfo as RemasterTerrain;
			if (terrainInfo == null)
				throw new InvalidDataException("TerrainRenderer is only compatible with the RemasterTerrain terrain parser.");

			tileCache = new RemasterTileCache(terrainInfo);
		}

		void IWorldLoaded.WorldLoaded(World world, WorldRenderer wr)
		{
			worldRenderer = wr;
			spriteLayers = new TerrainSpriteLayer[8];
			for (var i = 0; i < 8; i++)
				spriteLayers[i] = new TerrainSpriteLayer(world, wr, tileCache.MissingTile, BlendMode.Alpha, world.Type != WorldType.Editor);

			foreach (var cell in map.AllCells)
				UpdateCell(cell);

			map.Tiles.CellEntryChanged += UpdateCell;
			map.Height.CellEntryChanged += UpdateCell;
		}

		public void UpdateCell(CPos cell)
		{
			var tile = map.Tiles[cell];
			var palette = terrainInfo.Palette;
			if (terrainInfo.Templates.TryGetValue(tile.Type, out var template))
				palette = ((RemasterTerrainTemplateInfo)template).Palette ?? palette;

			var paletteReference = worldRenderer.Palette(palette);
			var scale = tileCache.TileScale(tile);
			for (var i = 0; i < 8; i++)
				spriteLayers[i].Update(cell, tileCache.TileSprite(tile, i), paletteReference, scale);
		}

		int t;
		void ITick.Tick(Actor self)
		{
			if (++t > 2)
			{
				t = 0;
				frame = (frame + 1) % 8;
			}
		}

		void IRenderTerrain.RenderTerrain(WorldRenderer wr, Viewport viewport)
		{
			spriteLayers[frame].Draw(wr.Viewport);
			foreach (var r in wr.World.WorldActor.TraitsImplementing<IRenderOverlay>())
				r.Render(wr);
		}

		void INotifyActorDisposing.Disposing(Actor self)
		{
			if (disposed)
				return;

			map.Tiles.CellEntryChanged -= UpdateCell;
			map.Height.CellEntryChanged -= UpdateCell;

			for (var i = 0; i < 8; i++)
				spriteLayers[i].Dispose();

			tileCache.Dispose();
			disposed = true;
		}

		Sprite ITiledTerrainRenderer.MissingTile { get { return tileCache.MissingTile; } }

		Sprite ITiledTerrainRenderer.TileSprite(TerrainTile r, int? variant)
		{
			return tileCache.TileSprite(r, variant ?? 0);
		}

		Rectangle ITiledTerrainRenderer.TemplateBounds(TerrainTemplateInfo template)
		{
			Rectangle? templateRect = null;
			var tileSize = map.Rules.TerrainInfo.TileSize;

			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					var tile = new TerrainTile(template.Id, (byte)i++);
					if (!terrainInfo.TryGetTileInfo(tile, out var tileInfo))
						continue;

					var sprite = tileCache.TileSprite(tile, 0);
					if (sprite == null)
						continue;

					var scale = tileCache.TileScale(tile);
					var u = map.Grid.Type == MapGridType.Rectangular ? x : (x - y) / 2f;
					var v = map.Grid.Type == MapGridType.Rectangular ? y : (x + y) / 2f;

					var tl = new Vector3(u * tileSize.Width, (v - 0.5f * tileInfo.Height) * tileSize.Height, 0) - 0.5f * scale * sprite.Size;
					var rect = new Rectangle(
						(int)(tl.X + scale * sprite.Offset.X),
						(int)(tl.Y + scale * sprite.Offset.Y),
						(int)(scale * sprite.Size.X),
						(int)(scale * sprite.Size.Y));

					templateRect = templateRect.HasValue ? Rectangle.Union(templateRect.Value, rect) : rect;
				}
			}

			return templateRect ?? Rectangle.Empty;
		}

		IEnumerable<IRenderable> ITiledTerrainRenderer.RenderUIPreview(WorldRenderer wr, TerrainTemplateInfo t, int2 origin, float scale)
		{
			if (t is not RemasterTerrainTemplateInfo template)
				yield break;

			var ts = map.Rules.TerrainInfo.TileSize;
			var gridType = map.Grid.Type;
			var palette = wr.Palette(template.Palette ?? terrainInfo.Palette);

			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					var tile = new TerrainTile(template.Id, (byte)i++);
					if (!terrainInfo.TryGetTileInfo(tile, out var tileInfo))
						continue;

					var sprite = tileCache.TileSprite(tile, 0);
					if (sprite == null)
						continue;

					var tileScale = tileCache.TileScale(tile);
					var u = gridType == MapGridType.Rectangular ? x : (x - y) / 2f;
					var v = gridType == MapGridType.Rectangular ? y : (x + y) / 2f;
					var offset = scale * new Vector2(u * ts.Width, (v - 0.5f * tileInfo.Height) * ts.Height) - 0.5f * scale * tileScale * sprite.Size.AsVector2();

					yield return new UISpriteRenderable(sprite, WPos.Zero, origin.ToVector2() + offset, 0, palette, scale * tileScale);
				}
			}
		}

		IEnumerable<IRenderable> ITiledTerrainRenderer.RenderPreview(WorldRenderer wr, TerrainTemplateInfo t, WPos origin)
		{
			if (t is not RemasterTerrainTemplateInfo template)
				yield break;

			var i = 0;
			var palette = wr.Palette(template.Palette ?? terrainInfo.Palette);
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++)
				{
					var tile = new TerrainTile(template.Id, (byte)i++);
					if (!terrainInfo.TryGetTileInfo(tile, out var tileInfo))
						continue;

					var sprite = tileCache.TileSprite(tile, 0);
					if (sprite == null)
						continue;

					var offset = map.Offset(new CVec(x, y), tileInfo.Height);
					var scale = tileCache.TileScale(tile);

					yield return new SpriteRenderable(sprite, origin, offset, 0, palette, scale, 1f, Vector3.One, TintModifiers.None, false);
				}
			}
		}

		IEnumerable<IRenderable> ITiledTerrainRenderer.RenderPreview(WorldRenderer wr, TerrainTile tile, WPos origin)
		{
			if (!terrainInfo.Templates.TryGetValue(tile.Type, out var template) || !template.Contains(tile.Index))
				yield break;

			var sprite = tileCache.TileSprite(tile, 0);
			if (sprite == null)
				yield break;

			var offset = map.Offset(new CVec(0, 0), template[tile.Index].Height);
			var scale = tileCache.TileScale(tile);
			var palette = wr.Palette((template as RemasterTerrainTemplateInfo)?.Palette ?? terrainInfo.Palette);
			yield return new SpriteRenderable(sprite, origin, offset, 0, palette, scale, 1f, Vector3.One, TintModifiers.None, false);
		}
	}
}
