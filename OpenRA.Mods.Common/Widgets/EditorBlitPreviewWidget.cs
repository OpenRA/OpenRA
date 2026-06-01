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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorBlitPreviewWidget : Widget
	{
		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly ITiledTerrainRenderer terrainRenderer;
		readonly DefaultTerrain terrainInfo;
		readonly IResourceRenderer[] resourceRenderers;

		Func<(EditorBlitSource Source, MapBlitFilters Filters)?> getPreview;
		IFinalizedRenderable[] renderables = [];

		[ObjectCreator.UseCtor]
		public EditorBlitPreviewWidget(WorldRenderer worldRenderer, World world)
		{
			this.worldRenderer = worldRenderer;
			this.world = world;

			terrainRenderer = world.WorldActor.TraitOrDefault<ITiledTerrainRenderer>();
			terrainInfo = (DefaultTerrain)world.Map.Rules.TerrainInfo;
			resourceRenderers = world.WorldActor.TraitsImplementing<IResourceRenderer>().ToArray();
		}

		protected EditorBlitPreviewWidget(EditorBlitPreviewWidget other)
			: base(other)
		{
			worldRenderer = other.worldRenderer;
			world = other.world;
			terrainRenderer = other.terrainRenderer;
			terrainInfo = other.terrainInfo;
			resourceRenderers = other.resourceRenderers;
			getPreview = other.getPreview;
		}

		public override EditorBlitPreviewWidget Clone() { return new EditorBlitPreviewWidget(this); }

		public void SetPreviewSource(Func<(EditorBlitSource Source, MapBlitFilters Filters)?> getPreview)
		{
			this.getPreview = getPreview;
		}

		public override void PrepareRenderables()
		{
			var preview = getPreview?.Invoke();
			if (preview == null || terrainRenderer == null)
			{
				renderables = [];
				return;
			}

			renderables = BuildRenderables(preview.Value.Source, preview.Value.Filters)
				.Select(r => r.PrepareRender(worldRenderer))
				.ToArray();
		}

		IEnumerable<IRenderable> BuildRenderables(EditorBlitSource source, MapBlitFilters filters)
		{
			var map = world.Map;
			var ts = terrainInfo.TileSize;
			var gridType = map.Grid.Type;
			var topLeft = source.CellCoords.TopLeft;
			var items = new List<(float2 Pos, float2 Size, Func<float, int2, IEnumerable<IRenderable>> Draw)>();

			if (filters.HasFlag(MapBlitFilters.Terrain) || filters.HasFlag(MapBlitFilters.Resources))
			{
				foreach (var (pos, tile) in source.Tiles)
				{
					var rel = pos - topLeft;
					if (filters.HasFlag(MapBlitFilters.Terrain) &&
						terrainInfo.TryGetTileInfo(tile.TerrainTile, out var tileInfo))
					{
						var sprite = terrainRenderer.TileSprite(tile.TerrainTile);
						var cellOffset = CellOffset(rel, tileInfo.Height, ts, gridType);
						var drawPos = cellOffset - 0.5f * sprite.Size.XY;
						var terrainTile = tile.TerrainTile;
						items.Add((drawPos, sprite.Size.XY, (scale, origin) =>
						{
							var palette = GetTerrainPalette(terrainTile);
							var uiOrigin = origin + (scale * drawPos).ToInt2();
							return [new UISpriteRenderable(sprite, WPos.Zero, uiOrigin, 0, palette, scale)];
						}));
					}

					if (filters.HasFlag(MapBlitFilters.Resources) &&
						tile.ResourceLayerContents is { Type: { } resourceType } &&
						!string.IsNullOrWhiteSpace(resourceType))
					{
						var resourceRenderer = resourceRenderers.FirstOrDefault(r => r.ResourceTypes.Contains(resourceType));
						if (resourceRenderer != null)
						{
							var cellOffset = CellOffset(rel, 0, ts, gridType);
							items.Add((cellOffset, new float2(ts.Width, ts.Height), (scale, origin) =>
								resourceRenderer.RenderUIPreview(worldRenderer, resourceType, origin + (scale * cellOffset).ToInt2(), scale)));
						}
					}
				}
			}

			if (filters.HasFlag(MapBlitFilters.Actors))
			{
				foreach (var (_, editorActorPreview) in source.Actors)
				{
					var actorRef = editorActorPreview.Export();
					if (!map.Rules.Actors.TryGetValue(actorRef.Type.ToLowerInvariant(), out var actorInfo))
						continue;

					var init = new ActorPreviewInitializer(actorRef, worldRenderer);
					var previews = actorInfo.TraitInfos<IRenderActorPreviewInfo>()
						.SelectMany(rpi => rpi.RenderPreview(init))
						.ToArray();

					var bounds = previews.SelectMany(p => p.ScreenBounds(worldRenderer, WPos.Zero)).Union();
					if (bounds.Width == 0 || bounds.Height == 0)
						continue;

					var rel = editorActorPreview.Location - topLeft;
					var cellOffset = CellOffset(rel, 0, ts, gridType);
					var drawPos = cellOffset + new float2(bounds.X, bounds.Y);
					items.Add((drawPos, new float2(bounds.Width, bounds.Height), (scale, origin) =>
					{
						var uiOrigin = origin + (scale * cellOffset).ToInt2();
						return previews.SelectMany(p => p.RenderUI(worldRenderer, uiOrigin, scale));
					}));
				}
			}

			if (items.Count == 0)
				yield break;

			var minX = items.Min(i => i.Pos.X);
			var minY = items.Min(i => i.Pos.Y);
			var maxX = items.Max(i => i.Pos.X + i.Size.X);
			var maxY = items.Max(i => i.Pos.Y + i.Size.Y);

			var contentWidth = maxX - minX;
			var contentHeight = maxY - minY;
			if (contentWidth <= 0 || contentHeight <= 0)
				yield break;

			var scale = Math.Min(RenderBounds.Width / contentWidth, RenderBounds.Height / contentHeight);
			var origin = new int2(
				RenderBounds.X + (int)((RenderBounds.Width - contentWidth * scale) / 2 - minX * scale),
				RenderBounds.Y + (int)((RenderBounds.Height - contentHeight * scale) / 2 - minY * scale));

			foreach (var item in items)
				foreach (var renderable in item.Draw(scale, origin))
					yield return renderable;
		}

		PaletteReference GetTerrainPalette(TerrainTile tile)
		{
			if (terrainInfo.Templates.TryGetValue(tile.Type, out var template) &&
				template is DefaultTerrainTemplateInfo defaultTemplate &&
				defaultTemplate.Palette != null)
				return worldRenderer.Palette(defaultTemplate.Palette);

			return worldRenderer.Palette(terrainInfo.Palette);
		}

		static float2 CellOffset(CVec cell, int height, Size tileSize, MapGridType gridType)
		{
			var u = gridType == MapGridType.Rectangular ? cell.X : (cell.X - cell.Y) / 2f;
			var v = gridType == MapGridType.Rectangular ? cell.Y : (cell.X + cell.Y) / 2f;
			return new float2(u * tileSize.Width, (v - 0.5f * height) * tileSize.Height);
		}

		public override void Draw()
		{
			Game.Renderer.EnableAntialiasingFilter();
			foreach (var renderable in renderables)
				renderable.Render(worldRenderer);
			Game.Renderer.DisableAntialiasingFilter();
		}
	}
}
