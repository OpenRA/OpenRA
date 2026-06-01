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
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets
{
	public class EditorBlitPreviewWidget : Widget
	{
		static readonly Color PlacedTileColor = Color.Yellow;
		static readonly Color MissingTileColor = Color.FromArgb(255, 255, 128, 0);

		// Must match BorderedRegionRenderable edge-to-neighbor mapping.
		static readonly ImmutableArray<(CVec Offset, int CornerIndex)> Offset2CornerIndex =
		[
			(new CVec(0, -1), 0),
			(new CVec(1, 0), 1),
			(new CVec(0, 1), 2),
			(new CVec(-1, 0), 3),
		];

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly ITiledTerrainRenderer terrainRenderer;
		readonly DefaultTerrain terrainInfo;
		readonly IResourceRenderer[] resourceRenderers;
		readonly IResourceLayer resourceLayer;

		Func<(EditorBlitSource Source, MapBlitFilters Filters)?> getPreview;
		Func<TemplatePlacementPreviewDisplay?> getPlacementDisplay;
		IFinalizedRenderable[] renderables = [];
		PreviewLayout layout;

		struct PreviewLayout
		{
			public float Scale;
			public int2 Origin;
			public CPos TopLeft;
			public bool Valid;
		}

		[ObjectCreator.UseCtor]
		public EditorBlitPreviewWidget(WorldRenderer worldRenderer, World world)
		{
			this.worldRenderer = worldRenderer;
			this.world = world;

			terrainRenderer = world.WorldActor.TraitOrDefault<ITiledTerrainRenderer>();
			terrainInfo = (DefaultTerrain)world.Map.Rules.TerrainInfo;
			resourceRenderers = world.WorldActor.TraitsImplementing<IResourceRenderer>().ToArray();
			resourceLayer = world.WorldActor.TraitOrDefault<IResourceLayer>();
		}

		protected EditorBlitPreviewWidget(EditorBlitPreviewWidget other)
			: base(other)
		{
			worldRenderer = other.worldRenderer;
			world = other.world;
			terrainRenderer = other.terrainRenderer;
			terrainInfo = other.terrainInfo;
			resourceRenderers = other.resourceRenderers;
			resourceLayer = other.resourceLayer;
			getPreview = other.getPreview;
			getPlacementDisplay = other.getPlacementDisplay;
		}

		public override EditorBlitPreviewWidget Clone() { return new EditorBlitPreviewWidget(this); }

		public void SetPreviewSource(Func<(EditorBlitSource Source, MapBlitFilters Filters)?> getPreview)
		{
			this.getPreview = getPreview;
		}

		public void SetPlacementDisplay(Func<TemplatePlacementPreviewDisplay?> getPlacementDisplay)
		{
			this.getPlacementDisplay = getPlacementDisplay;
		}

		public override void PrepareRenderables()
		{
			layout = default;
			var preview = getPreview?.Invoke();
			if (preview == null || terrainRenderer == null)
			{
				renderables = [];
				return;
			}

			var (renderablesList, previewLayout) = BuildRenderables(
				preview.Value.Source,
				preview.Value.Filters,
				getPlacementDisplay?.Invoke());

			layout = previewLayout;
			renderables = renderablesList
				.Select(r => r.PrepareRender(worldRenderer))
				.ToArray();
		}

		(IEnumerable<IRenderable> Renderables, PreviewLayout Layout) BuildRenderables(
			EditorBlitSource source,
			MapBlitFilters filters,
			TemplatePlacementPreviewDisplay? placementDisplay)
		{
			var map = world.Map;
			var ts = terrainInfo.TileSize;
			var gridType = map.Grid.Type;
			var topLeft = source.CellCoords.TopLeft;
			var sourceTiles = new HashSet<CPos>(source.Tiles.Keys);
			var placementPreview = placementDisplay?.Placement;
			var useOriginalTemplate = placementDisplay?.Mode == TilePlacementPreviewDisplayMode.Original;

			if (placementPreview.HasValue
				&& terrainInfo is ITemplatedTerrainInfo templatedTerrainInfo
				&& templatedTerrainInfo.Templates.TryGetValue(placementPreview.Value.TemplateType, out var placementTemplate)
				&& !placementTemplate.PickAny
				&& (placementTemplate.Size.X != 1 || placementTemplate.Size.Y != 1))
			{
				topLeft = placementPreview.Value.Anchor;
			}

			var items = new List<(float2 Pos, float2 Size, Func<float, int2, IEnumerable<IRenderable>> Draw)>();

			if (filters.HasFlag(MapBlitFilters.Terrain) || filters.HasFlag(MapBlitFilters.Resources))
			{
				if (useOriginalTemplate
					&& placementPreview.HasValue
					&& terrainInfo is ITemplatedTerrainInfo originalTerrainInfo
					&& originalTerrainInfo.Templates.TryGetValue(placementPreview.Value.TemplateType, out var originalTemplate))
				{
					AddOriginalTemplateTiles(
						originalTemplate,
						placementPreview.Value,
						topLeft,
						filters,
						ts,
						gridType,
						items);
				}
				else
				{
					AddTilesFromSource(source, filters, topLeft, ts, gridType, items);

					if (placementPreview.HasValue
						&& terrainInfo is ITemplatedTerrainInfo placementTerrainInfo
						&& placementTerrainInfo.Templates.TryGetValue(placementPreview.Value.TemplateType, out var template))
					{
						foreach (var cell in TemplateBoundsOverlay.BuildTemplateFootprintCells(template, placementPreview.Value.Anchor))
						{
							if (sourceTiles.Contains(cell))
								continue;

							if (!map.Tiles.Contains(cell))
								continue;

							AddMapTile(cell, cell - topLeft, filters, ts, gridType, items);
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
				return ([], default);

			var minX = items.Min(i => i.Pos.X);
			var minY = items.Min(i => i.Pos.Y);
			var maxX = items.Max(i => i.Pos.X + i.Size.X);
			var maxY = items.Max(i => i.Pos.Y + i.Size.Y);

			var contentWidth = maxX - minX;
			var contentHeight = maxY - minY;
			if (contentWidth <= 0 || contentHeight <= 0)
				return ([], default);

			var scale = Math.Min(RenderBounds.Width / contentWidth, RenderBounds.Height / contentHeight);
			var origin = new int2(
				RenderBounds.X + (int)((RenderBounds.Width - contentWidth * scale) / 2 - minX * scale),
				RenderBounds.Y + (int)((RenderBounds.Height - contentHeight * scale) / 2 - minY * scale));

			var renderables = new List<IRenderable>();
			foreach (var item in items)
				renderables.AddRange(item.Draw(scale, origin));

			return (renderables, new PreviewLayout
			{
				Scale = scale,
				Origin = origin,
				TopLeft = topLeft,
				Valid = true
			});
		}

		void AddOriginalTemplateTiles(
			TerrainTemplateInfo template,
			TemplatePlacementPreview placementPreview,
			CPos topLeft,
			MapBlitFilters filters,
			Size ts,
			MapGridType gridType,
			List<(float2 Pos, float2 Size, Func<float, int2, IEnumerable<IRenderable>> Draw)> items)
		{
			if (!filters.HasFlag(MapBlitFilters.Terrain))
				return;

			var templateWidth = template.Size.X;
			var templateType = placementPreview.TemplateType;
			for (byte i = 0; i < templateWidth * template.Size.Y; i++)
			{
				if (!template.Contains(i) || template[i] == null)
					continue;

				var cell = placementPreview.Anchor + new CVec(i % templateWidth, i / templateWidth);
				var terrainTile = new TerrainTile(templateType, i);
				var height = template[i].Height;
				AddBlitTile(
					cell,
					cell - topLeft,
					new BlitTile(terrainTile, default, null, height),
					filters,
					ts,
					gridType,
					items);
			}
		}

		void AddTilesFromSource(
			EditorBlitSource source,
			MapBlitFilters filters,
			CPos topLeft,
			Size ts,
			MapGridType gridType,
			List<(float2 Pos, float2 Size, Func<float, int2, IEnumerable<IRenderable>> Draw)> items)
		{
			foreach (var (pos, tile) in source.Tiles)
				AddBlitTile(pos, pos - topLeft, tile, filters, ts, gridType, items);
		}

		void AddMapTile(
			CPos pos,
			CVec rel,
			MapBlitFilters filters,
			Size ts,
			MapGridType gridType,
			List<(float2 Pos, float2 Size, Func<float, int2, IEnumerable<IRenderable>> Draw)> items)
		{
			var map = world.Map;
			var terrainTile = map.Tiles[pos];
			var height = map.Height.Contains(pos.ToMPos(map)) ? map.Height[pos] : (byte)0;
			AddBlitTile(
				pos,
				rel,
				new BlitTile(terrainTile, map.Resources[pos], resourceLayer?.GetResource(pos), height),
				filters,
				ts,
				gridType,
				items);
		}

		void AddBlitTile(
			CPos pos,
			CVec rel,
			BlitTile tile,
			MapBlitFilters filters,
			Size ts,
			MapGridType gridType,
			List<(float2 Pos, float2 Size, Func<float, int2, IEnumerable<IRenderable>> Draw)> items)
		{
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

			DrawPlacementOverlays();
			Game.Renderer.DisableAntialiasingFilter();
		}

		void DrawPlacementOverlays()
		{
			if (!layout.Valid)
				return;

			var placementDisplay = getPlacementDisplay?.Invoke();
			if (!placementDisplay.HasValue)
				return;

			var placementPreview = placementDisplay.Value.Placement;

			if (terrainInfo is not ITemplatedTerrainInfo templatedTerrainInfo)
				return;

			if (!templatedTerrainInfo.Templates.TryGetValue(placementPreview.TemplateType, out var template))
				return;

			if (template.PickAny || (template.Size.X == 1 && template.Size.Y == 1))
				return;

			var map = world.Map;
			var anchor = placementPreview.Anchor;
			var templateType = placementPreview.TemplateType;
			var matching = TemplateBoundsOverlay.BuildMatchingTemplateRegion(template, anchor, templateType, map);
			var missing = TemplateBoundsOverlay.BuildMissingTemplateSlots(template, anchor, templateType, map);

			var cr = Game.Renderer.RgbaColorRenderer;

			if (matching.Length > 0)
				DrawRegionBorder(cr, map, matching, layout, PlacedTileColor, 1f, dashed: false);

			if (missing.Length > 0)
				DrawRegionBorder(cr, map, missing, layout, MissingTileColor, 1f, dashed: true);
		}

		void DrawRegionBorder(
			RgbaColorRenderer cr,
			Map map,
			CPos[] region,
			PreviewLayout previewLayout,
			Color color,
			float width,
			bool dashed)
		{
			var regionSet = region.ToHashSet();
			var tileSize = terrainInfo.TileSize;
			var gridType = map.Grid.Type;
			var tileScale = map.Grid.TileScale;

			foreach (var cell in region)
			{
				var mpos = cell.ToMPos(map);
				if (!map.Height.Contains(mpos))
					continue;

				var tile = map.Tiles[cell];
				var ti = map.Rules.TerrainInfo.GetTerrainInfo(tile);
				var ramp = ti?.RampType ?? 0;
				var corners = map.Grid.Ramps[ramp].Corners;
				var height = map.Height[mpos];
				var rel = cell - previewLayout.TopLeft;
				var center = (float2)previewLayout.Origin + previewLayout.Scale * CellOffset(rel, height, tileSize, gridType);

				foreach (var (offset, cornerIndex) in Offset2CornerIndex)
				{
					if (regionSet.Contains(cell + offset))
						continue;

					var start = center + PreviewCornerOffset(corners[cornerIndex], tileSize, gridType, tileScale, previewLayout.Scale);
					var end = center + PreviewCornerOffset(corners[(cornerIndex + 1) % 4], tileSize, gridType, tileScale, previewLayout.Scale);

					if (dashed)
						DrawDashedLine(cr, start, end, width, color);
					else
						cr.DrawLine(start, end, width, color);
				}
			}
		}

		static float2 PreviewCornerOffset(WVec corner, Size tileSize, MapGridType gridType, int tileScale, float scale)
		{
			if (gridType == MapGridType.RectangularIsometric)
			{
				return scale * new float2(
					(corner.X - corner.Y) / (float)tileScale * tileSize.Width,
					(corner.X + corner.Y) / (float)tileScale * tileSize.Height);
			}

			return scale * new float2(
				corner.X / (float)tileScale * tileSize.Width,
				corner.Y / (float)tileScale * tileSize.Height);
		}

		static void DrawDashedLine(RgbaColorRenderer cr, float2 start, float2 end, float width, Color color, float dashLength = 4f)
		{
			var delta = end - start;
			var length = delta.Length;
			if (length < 1f)
				return;

			var direction = delta / length;
			var position = 0f;
			var draw = true;
			while (position < length)
			{
				var segmentLength = Math.Min(dashLength, length - position);
				if (draw)
				{
					var segmentStart = start + position * direction;
					var segmentEnd = start + (position + segmentLength) * direction;
					cr.DrawLine(segmentStart, segmentEnd, width, color);
				}

				position += segmentLength;
				draw = !draw;
			}
		}
	}
}
