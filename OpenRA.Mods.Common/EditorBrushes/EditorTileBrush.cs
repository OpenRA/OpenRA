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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.EditorBrushes;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class EditorTileBrush : IEditorBrush
	{
		public readonly TerrainTemplateInfo TerrainTemplate;
		public readonly ushort Template;
		public readonly ushort[] Templates;

		readonly WorldRenderer worldRenderer;
		readonly World world;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly EditorViewportControllerWidget editorWidget;
		readonly EditorActionManager editorActionManager;

		bool painting;

		readonly ITiledTerrainRenderer terrainRenderer;

		CPos cell;
		readonly List<IRenderable> preview = [];
		int nextTemplate;

		public EditorTileBrush(EditorViewportControllerWidget editorWidget, ushort id, WorldRenderer wr)
			: this(editorWidget, [id], wr) { }

		public EditorTileBrush(EditorViewportControllerWidget editorWidget, IEnumerable<ushort> ids, WorldRenderer wr)
		{
			this.editorWidget = editorWidget;
			worldRenderer = wr;
			world = wr.World;
			terrainInfo = world.Map.Rules.TerrainInfo as ITemplatedTerrainInfo;
			if (terrainInfo == null)
				throw new InvalidDataException($"{nameof(EditorTileBrush)} can only be used with template-based tilesets");

			editorActionManager = world.WorldActor.Trait<EditorActionManager>();
			terrainRenderer = world.WorldActor.Trait<ITiledTerrainRenderer>();

			Templates = ids.Distinct().ToArray();
			Template = Templates[0];
			TerrainTemplate = terrainInfo.Templates[Template];
			cell = wr.Viewport.ViewToWorld(wr.Viewport.WorldToViewPx(Viewport.LastMousePos));
			UpdatePreview();
		}

		public bool HandleMouseInput(MouseInput mi)
		{
			// Exclusively uses left and right mouse buttons, but nothing else
			if (mi.Button != MouseButton.Left && mi.Button != MouseButton.Right)
				return false;

			if (mi.Button == MouseButton.Right)
			{
				if (mi.Event == MouseInputEvent.Up)
				{
					editorWidget.ClearBrush();
					return true;
				}

				return false;
			}

			if (mi.Button == MouseButton.Left)
			{
				if (mi.Event == MouseInputEvent.Down)
					painting = true;
				else if (mi.Event == MouseInputEvent.Up)
					painting = false;
			}

			if (!painting)
				return true;

			if (mi.Event != MouseInputEvent.Down && mi.Event != MouseInputEvent.Move)
				return true;

			var cell = worldRenderer.Viewport.ViewToWorld(mi.Location);
			var isMoving = mi.Event == MouseInputEvent.Move;

			if (mi.Modifiers.HasModifier(Modifiers.Shift))
			{
				FloodFillWithBrush(cell);
				painting = false;
			}
			else
				PaintCell(cell, isMoving);

			return true;
		}

		void PaintCell(CPos cell, bool isMoving)
		{
			var templateId = PickTemplate();
			var template = terrainInfo.Templates[templateId];
			if (isMoving && PlacementOverlapsSameTemplate(template, cell))
				return;

			editorActionManager.Add(new PaintTileEditorAction(templateId, world.Map, cell));
		}

		void FloodFillWithBrush(CPos cell)
		{
			var map = world.Map;
			if (!map.Contains(cell))
				return;

			var mapTiles = map.Tiles;
			var replace = mapTiles[cell];
			var template = PickTemplate();

			if (replace.Type == template)
				return;

			editorActionManager.Add(new FloodFillEditorAction(template, map, cell));
		}

		ushort PickTemplate()
		{
			if (Templates.Length == 1)
				return Template;

			if (editorWidget.AssetMixMode == EditorAssetMixMode.Sequential)
				return Templates[nextTemplate++ % Templates.Length];

			return Templates[Game.CosmeticRandom.Next(Templates.Length)];
		}

		bool PlacementOverlapsSameTemplate(TerrainTemplateInfo template, CPos cell)
		{
			var map = world.Map;
			var mapTiles = map.Tiles;
			var i = 0;
			for (var y = 0; y < template.Size.Y; y++)
			{
				for (var x = 0; x < template.Size.X; x++, i++)
				{
					if (template.Contains(i) && template[i] != null)
					{
						var c = cell + new CVec(x, y);
						if (mapTiles.Contains(c) && mapTiles[c].Type == template.Id)
							return true;
					}
				}
			}

			return false;
		}

		void UpdatePreview()
		{
			var pos = world.Map.CenterOfCell(cell);

			preview.Clear();
			preview.AddRange(terrainRenderer.RenderPreview(worldRenderer, TerrainTemplate, pos));
		}

		void IEditorBrush.TickRender(WorldRenderer wr, Actor self)
		{
			var currentCell = wr.Viewport.ViewToWorld(Viewport.LastMousePos);
			if (cell != currentCell)
			{
				cell = currentCell;
				UpdatePreview();
			}
		}

		IEnumerable<IRenderable> IEditorBrush.RenderAboveShroud(Actor self, WorldRenderer wr) { return preview; }
		IEnumerable<IRenderable> IEditorBrush.RenderAnnotations(Actor self, WorldRenderer wr) { yield break; }

		public void Tick() { }

		public void Dispose() { }
	}

	sealed class PaintTileEditorAction : IEditorAction
	{
		[FluentReference("id")]
		const string AddedTile = "notification-added-tile";

		public string Text { get; }

		readonly ushort template;
		readonly Map map;
		readonly CPos cell;

		readonly Queue<UndoTile> undoTiles = [];
		readonly TerrainTemplateInfo terrainTemplate;

		public PaintTileEditorAction(ushort template, Map map, CPos cell)
		{
			this.template = template;
			this.map = map;
			this.cell = cell;

			var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			terrainTemplate = terrainInfo.Templates[template];
			Text = FluentProvider.GetMessage(AddedTile, "id", terrainTemplate.Id);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cell) ? mapHeight[cell] : (byte)0;

			var i = 0;
			for (var y = 0; y < terrainTemplate.Size.Y; y++)
			{
				for (var x = 0; x < terrainTemplate.Size.X; x++, i++)
				{
					if (terrainTemplate.Contains(i) && terrainTemplate[i] != null)
					{
						var index = terrainTemplate.PickAny ? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount) : (byte)i;
						var c = cell + new CVec(x, y);
						if (!mapTiles.Contains(c))
							continue;

						undoTiles.Enqueue(new UndoTile(c, mapTiles[c], mapHeight[c]));

						mapTiles[c] = new TerrainTile(template, index);
						mapHeight[c] = (byte)(baseHeight + terrainTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					}
				}
			}
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();

				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}
	}

	sealed class FloodFillEditorAction : IEditorAction
	{
		[FluentReference("id")]
		const string FilledTile = "notification-filled-tile";

		public string Text { get; }

		readonly ushort template;
		readonly Map map;
		readonly CPos cell;

		readonly Queue<UndoTile> undoTiles = [];
		readonly TerrainTemplateInfo terrainTemplate;

		public FloodFillEditorAction(ushort template, Map map, CPos cell)
		{
			this.template = template;
			this.map = map;
			this.cell = cell;

			var terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			terrainTemplate = terrainInfo.Templates[template];
			Text = FluentProvider.GetMessage(FilledTile, "id", terrainTemplate.Id);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			var queue = new Queue<CPos>();
			var touched = new CellLayer<bool>(map);
			var mapTiles = map.Tiles;
			var replace = mapTiles[cell];

			void MaybeEnqueue(CPos newCell)
			{
				if (map.Contains(cell) && !touched[newCell])
				{
					queue.Enqueue(newCell);
					touched[newCell] = true;
				}
			}

			bool ShouldPaint(CPos cellToCheck)
			{
				for (var y = 0; y < terrainTemplate.Size.Y; y++)
				{
					for (var x = 0; x < terrainTemplate.Size.X; x++)
					{
						var c = cellToCheck + new CVec(x, y);
						if (!map.Contains(c) || mapTiles[c].Type != replace.Type)
							return false;
					}
				}

				return true;
			}

			CPos FindEdge(CPos refCell, CVec direction)
			{
				while (true)
				{
					var newCell = refCell + direction;
					if (!ShouldPaint(newCell))
						return refCell;
					refCell = newCell;
				}
			}

			queue.Enqueue(cell);
			while (queue.Count > 0)
			{
				var queuedCell = queue.Dequeue();
				if (!ShouldPaint(queuedCell))
					continue;

				var previousCell = FindEdge(queuedCell, new CVec(-1 * terrainTemplate.Size.X, 0));
				var nextCell = FindEdge(queuedCell, new CVec(1 * terrainTemplate.Size.X, 0));

				for (var x = previousCell.X; x <= nextCell.X; x += terrainTemplate.Size.X)
				{
					PaintSingleCell(new CPos(x, queuedCell.Y));
					var upperCell = new CPos(x, queuedCell.Y - 1 * terrainTemplate.Size.Y);
					var lowerCell = new CPos(x, queuedCell.Y + 1 * terrainTemplate.Size.Y);

					if (ShouldPaint(upperCell))
						MaybeEnqueue(upperCell);
					if (ShouldPaint(lowerCell))
						MaybeEnqueue(lowerCell);
				}
			}
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();

				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}

		void PaintSingleCell(CPos cellToPaint)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cellToPaint) ? mapHeight[cellToPaint] : (byte)0;

			var i = 0;
			for (var y = 0; y < terrainTemplate.Size.Y; y++)
			{
				for (var x = 0; x < terrainTemplate.Size.X; x++, i++)
				{
					if (terrainTemplate.Contains(i) && terrainTemplate[i] != null)
					{
						var index = terrainTemplate.PickAny ? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount) : (byte)i;
						var c = cellToPaint + new CVec(x, y);
						if (!mapTiles.Contains(c))
							continue;

						undoTiles.Enqueue(new UndoTile(c, mapTiles[c], mapHeight[c]));

						mapTiles[c] = new TerrainTile(template, index);
						mapHeight[c] = (byte)(baseHeight + terrainTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					}
				}
			}
		}
	}

	sealed class FillSelectionWithTileEditorAction : IEditorAction
	{
		enum FillAxis { Horizontal, Vertical, Grid }

		[FluentReference("id")]
		const string FilledTile = "notification-filled-tile";

		public string Text { get; }

		readonly ushort[] templates;
		readonly EditorAssetMixMode mixMode;
		readonly int fillDensityPercent;
		readonly Map map;
		readonly CellCoordsRegion area;
		readonly IReadOnlySet<CPos> mask;
		readonly IReadOnlySet<CPos> paintMask;

		readonly Queue<UndoTile> undoTiles = [];
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly TerrainTemplateInfo firstTerrainTemplate;
		readonly byte roadTerrainIndex;
		readonly Dictionary<ushort, EditorTemplateRoadAlignment.Profile> roadProfiles;
		readonly FillAxis fillAxis;
		readonly int targetRoadX;
		readonly int targetRoadY;
		int nextTemplate;

		public FillSelectionWithTileEditorAction(ushort template, Map map, CellCoordsRegion area, IReadOnlySet<CPos> mask = null)
			: this([template], EditorAssetMixMode.Random, 100, map, area, mask) { }

		public FillSelectionWithTileEditorAction(
			IEnumerable<ushort> templates,
			EditorAssetMixMode mixMode,
			int fillDensityPercent,
			Map map,
			CellCoordsRegion area,
			IReadOnlySet<CPos> mask = null,
			IReadOnlySet<CPos> paintMask = null)
		{
			this.templates = templates.Distinct().ToArray();
			this.mixMode = mixMode;
			this.fillDensityPercent = fillDensityPercent.Clamp(10, 100);
			this.map = map;
			this.area = area;
			this.mask = mask;
			this.paintMask = paintMask;

			terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			firstTerrainTemplate = terrainInfo.Templates[this.templates[0]];
			Text = FluentProvider.GetMessage(FilledTile, "id", firstTerrainTemplate.Id);

			roadTerrainIndex = terrainInfo.GetTerrainIndex("Road");
			roadProfiles = this.templates.ToDictionary(
				t => t,
				t => EditorTemplateRoadAlignment.GetProfile(terrainInfo, roadTerrainIndex, terrainInfo.Templates[t]));

			var width = area.BottomRight.X - area.TopLeft.X + 1;
			var height = area.BottomRight.Y - area.TopLeft.Y + 1;
			if (height > width)
				fillAxis = FillAxis.Vertical;
			else if (width > height)
				fillAxis = FillAxis.Horizontal;
			else
				fillAxis = FillAxis.Grid;

			targetRoadX = (area.TopLeft.X + area.BottomRight.X) / 2;
			targetRoadY = (area.TopLeft.Y + area.BottomRight.Y) / 2;
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			if (mask != null && !HasMultiCellTemplate())
			{
				var maskAnchors = mask.ToList();
				foreach (var cell in EditorFillSelection.SelectCells(maskAnchors, fillDensityPercent))
					PaintTemplate(PickTemplate(), cell);
				return;
			}

			var placements = BuildContiguousPlacements();
			var placementAnchors = new List<CPos>(placements.Count);
			foreach (var placement in placements)
				placementAnchors.Add(placement.Anchor);

			var selected = new HashSet<CPos>(EditorFillSelection.SelectCells(placementAnchors, fillDensityPercent));
			foreach (var (anchor, template) in placements)
			{
				if (!selected.Contains(anchor))
					continue;

				if (mask != null && !mask.Contains(anchor))
					continue;

				PaintTemplate(template, anchor);
			}
		}

		List<(CPos Anchor, ushort Template)> BuildContiguousPlacements()
		{
			// Road alignment only applies to templates that contain road cells; terrain like water
			// should tile across the full selection from the top-left corner.
			if (templates.All(t => !roadProfiles[t].HasRoad))
				return BuildGridPlacements();

			return fillAxis switch
			{
				FillAxis.Vertical => BuildVerticalPlacements(),
				FillAxis.Horizontal => BuildHorizontalPlacements(),
				_ => BuildGridPlacements(),
			};
		}

		List<(CPos Anchor, ushort Template)> BuildVerticalPlacements()
		{
			var placements = new List<(CPos, ushort)>();
			var y = area.TopLeft.Y;
			while (y <= area.BottomRight.Y)
			{
				var template = PickTemplate();
				var terrainTemplate = terrainInfo.Templates[template];
				var profile = roadProfiles[template];
				var anchorX = targetRoadX - (int)Math.Round(profile.RoadCenterX);
				placements.Add((new CPos(anchorX, y), template));
				y += terrainTemplate.Size.Y;
			}

			return placements;
		}

		List<(CPos Anchor, ushort Template)> BuildHorizontalPlacements()
		{
			var placements = new List<(CPos, ushort)>();
			var x = area.TopLeft.X;
			while (x <= area.BottomRight.X)
			{
				var template = PickTemplate();
				var terrainTemplate = terrainInfo.Templates[template];
				var profile = roadProfiles[template];
				var anchorY = targetRoadY - (int)Math.Round(profile.RoadCenterY);
				placements.Add((new CPos(x, anchorY), template));
				x += terrainTemplate.Size.X;
			}

			return placements;
		}

		List<(CPos Anchor, ushort Template)> BuildGridPlacements()
		{
			var placements = new List<(CPos, ushort)>();
			var y = area.TopLeft.Y;
			while (y <= area.BottomRight.Y)
			{
				var x = area.TopLeft.X;
				var rowMaxY = 0;
				while (x <= area.BottomRight.X)
				{
					var template = PickTemplate();
					var terrainTemplate = terrainInfo.Templates[template];
					var profile = roadProfiles[template];
					rowMaxY = Math.Max(rowMaxY, terrainTemplate.Size.Y);
					var anchorY = profile.HasRoad
						? targetRoadY - (int)Math.Round(profile.RoadCenterY)
						: y;
					placements.Add((new CPos(x, anchorY), template));
					x += terrainTemplate.Size.X;
				}

				y += Math.Max(rowMaxY, 1);
			}

			return placements;
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();

				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}

		ushort PickTemplate()
		{
			var pool = templates;
			if (templates.Length > 1)
			{
				var oriented = templates.Where(t =>
				{
					var profile = roadProfiles[t];
					if (!profile.HasRoad)
						return true;

					var roadWidth = profile.MaxRoadX - profile.MinRoadX;
					var roadHeight = profile.MaxRoadY - profile.MinRoadY;
					return fillAxis switch
					{
						FillAxis.Vertical => roadHeight >= roadWidth,
						FillAxis.Horizontal => roadWidth >= roadHeight,
						_ => true,
					};
				}).ToArray();

				if (oriented.Length > 0)
					pool = oriented;
			}

			if (pool.Length == 1)
				return pool[0];

			if (mixMode == EditorAssetMixMode.Sequential)
				return pool[nextTemplate++ % pool.Length];

			return pool[Game.CosmeticRandom.Next(pool.Length)];
		}

		bool HasMultiCellTemplate()
		{
			return templates.Any(t =>
			{
				var size = terrainInfo.Templates[t].Size;
				return size.X > 1 || size.Y > 1;
			});
		}

		void PaintTemplate(ushort template, CPos cellToPaint)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cellToPaint) ? mapHeight[cellToPaint] : (byte)0;
			var terrainTemplate = terrainInfo.Templates[template];

			var i = 0;
			for (var y = 0; y < terrainTemplate.Size.Y; y++)
			{
				for (var x = 0; x < terrainTemplate.Size.X; x++, i++)
				{
					if (terrainTemplate.Contains(i) && terrainTemplate[i] != null)
					{
						var index = terrainTemplate.PickAny ? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount) : (byte)i;
						var c = cellToPaint + new CVec(x, y);
						if (!area.Contains(c) || !mapTiles.Contains(c))
							continue;

						if (paintMask != null && !paintMask.Contains(c))
							continue;

						undoTiles.Enqueue(new UndoTile(c, mapTiles[c], mapHeight[c]));

						mapTiles[c] = new TerrainTile(template, index);
						mapHeight[c] = (byte)(baseHeight + terrainTemplate[index].Height).Clamp(0, map.Grid.MaximumTerrainHeight);
					}
				}
			}
		}
	}

	sealed class ReplaceTemplatePlacementsEditorAction : IEditorAction
	{
		[FluentReference("count", "id")]
		const string ReplacedTiles = "notification-replaced-tiles";

		public string Text { get; }

		readonly ushort[] templates;
		readonly EditorAssetMixMode mixMode;
		readonly Map map;
		readonly ITemplatedTerrainInfo terrainInfo;
		readonly TerrainTile defaultTile;
		readonly List<CPos> intactAnchors;
		readonly HashSet<CPos> packCells;
		readonly Queue<UndoTile> undoTiles = [];
		int nextTemplate;

		public ReplaceTemplatePlacementsEditorAction(
			IEnumerable<ushort> templates,
			EditorAssetMixMode mixMode,
			Map map,
			TileReplacePlan plan)
		{
			this.templates = templates.Distinct().ToArray();
			this.mixMode = mixMode;
			this.map = map;
			intactAnchors = plan.IntactAnchors;
			packCells = plan.PackCells;
			terrainInfo = (ITemplatedTerrainInfo)map.Rules.TerrainInfo;
			defaultTile = map.Rules.TerrainInfo.DefaultTerrainTile;
			var placementCount = intactAnchors.Count + packCells.Count;
			Text = FluentProvider.GetMessage(
				ReplacedTiles,
				"count", placementCount,
				"id", terrainInfo.Templates[this.templates[0]].Id);
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			foreach (var anchor in intactAnchors)
			{
				var template = PickTemplate();
				var terrainTemplate = terrainInfo.Templates[template];
				ClearPlacedTemplate(anchor);
				PaintTemplate(template, terrainTemplate, anchor);
			}

			if (packCells.Count == 0)
				return;

			foreach (var cluster in BuildConnectedClusters(packCells))
				ReplaceCluster(cluster);
		}

		void ReplaceCluster(HashSet<CPos> cluster)
		{
			var placed = new HashSet<CPos>();
			var origin = new CPos(cluster.Min(c => c.X), cluster.Min(c => c.Y));

			if (HasMultiCellTemplate())
			{
				var bounds = ClusterBounds(cluster);
				for (var anchorY = bounds.MinY; anchorY <= bounds.MaxY; anchorY++)
				{
					for (var anchorX = bounds.MinX; anchorX <= bounds.MaxX; anchorX++)
					{
						var template = PickTemplate();
						var terrainTemplate = terrainInfo.Templates[template];
						var anchor = new CPos(anchorX, anchorY);
						if (!CanPlaceIntactTemplate(anchor, terrainTemplate, cluster, placed))
							continue;

						PaintIntactTemplate(template, terrainTemplate, anchor, cluster);
						foreach (var cell in TemplateBoundsOverlay.BuildTemplateFootprintCells(terrainTemplate, anchor))
							placed.Add(cell);
					}
				}
			}

			foreach (var cell in cluster)
			{
				if (placed.Contains(cell))
					continue;

				var template = PickTemplate();
				var terrainTemplate = terrainInfo.Templates[template];
				PaintTemplateFragment(template, terrainTemplate, cell, origin);
			}
		}

		bool HasMultiCellTemplate()
		{
			return templates.Any(t =>
			{
				var size = terrainInfo.Templates[t].Size;
				return size.X > 1 || size.Y > 1;
			});
		}

		static (int MinX, int MinY, int MaxX, int MaxY) ClusterBounds(HashSet<CPos> cluster)
		{
			return (
				cluster.Min(c => c.X),
				cluster.Min(c => c.Y),
				cluster.Max(c => c.X),
				cluster.Max(c => c.Y));
		}

		static bool CanPlaceIntactTemplate(
			CPos anchor,
			TerrainTemplateInfo template,
			HashSet<CPos> cluster,
			HashSet<CPos> placed)
		{
			var footprint = TemplateBoundsOverlay.BuildTemplateFootprintCells(template, anchor);
			if (footprint.Length == 0)
				return false;

			foreach (var cell in footprint)
			{
				if (!cluster.Contains(cell))
					return false;

				if (placed.Contains(cell))
					return false;
			}

			return true;
		}

		void PaintIntactTemplate(
			ushort template,
			TerrainTemplateInfo terrainTemplate,
			CPos anchor,
			HashSet<CPos> allowedCells)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(anchor) ? mapHeight[anchor] : (byte)0;

			var i = 0;
			for (var y = 0; y < terrainTemplate.Size.Y; y++)
			{
				for (var x = 0; x < terrainTemplate.Size.X; x++, i++)
				{
					if (!terrainTemplate.Contains(i) || terrainTemplate[i] == null)
						continue;

					var c = anchor + new CVec(x, y);
					if (!allowedCells.Contains(c) || !mapTiles.Contains(c))
						continue;

					var index = terrainTemplate.PickAny
						? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount)
						: (byte)i;

					SaveUndoCell(c);
					mapTiles[c] = new TerrainTile(template, index);
					mapHeight[c] = (byte)(baseHeight + terrainTemplate[index].Height)
						.Clamp(0, map.Grid.MaximumTerrainHeight);
				}
			}
		}

		void PaintTemplateFragment(
			ushort template,
			TerrainTemplateInfo terrainTemplate,
			CPos cell,
			CPos clusterOrigin)
		{
			if (!map.Tiles.Contains(cell))
				return;

			var localX = Mod(cell.X - clusterOrigin.X, terrainTemplate.Size.X);
			var localY = Mod(cell.Y - clusterOrigin.Y, terrainTemplate.Size.Y);
			var index = localY * terrainTemplate.Size.X + localX;
			if (!terrainTemplate.Contains(index) || terrainTemplate[index] == null)
				return;

			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cell) ? mapHeight[cell] : (byte)0;
			var tileIndex = terrainTemplate.PickAny
				? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount)
				: (byte)index;

			SaveUndoCell(cell);
			mapTiles[cell] = new TerrainTile(template, tileIndex);
			mapHeight[cell] = (byte)(baseHeight + terrainTemplate[index].Height)
				.Clamp(0, map.Grid.MaximumTerrainHeight);
		}

		static int Mod(int value, int size)
		{
			if (size <= 0)
				return 0;

			var result = value % size;
			return result < 0 ? result + size : result;
		}

		static List<HashSet<CPos>> BuildConnectedClusters(HashSet<CPos> cells)
		{
			var remaining = new HashSet<CPos>(cells);
			var clusters = new List<HashSet<CPos>>();
			var directions = new[] { new CVec(1, 0), new CVec(-1, 0), new CVec(0, 1), new CVec(0, -1) };

			while (remaining.Count > 0)
			{
				var start = remaining.OrderBy(c => c.Y).ThenBy(c => c.X).First();
				var cluster = new HashSet<CPos>();
				var queue = new Queue<CPos>();
				queue.Enqueue(start);
				remaining.Remove(start);
				cluster.Add(start);

				while (queue.Count > 0)
				{
					var current = queue.Dequeue();
					foreach (var direction in directions)
					{
						var next = current + direction;
						if (!remaining.Remove(next))
							continue;

						cluster.Add(next);
						queue.Enqueue(next);
					}
				}

				clusters.Add(cluster);
			}

			return clusters;
		}

		public void Undo()
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;

			while (undoTiles.Count > 0)
			{
				var undoTile = undoTiles.Dequeue();
				mapTiles[undoTile.Cell] = undoTile.MapTile;
				mapHeight[undoTile.Cell] = undoTile.Height;
			}
		}

		void ClearPlacedTemplate(CPos anchor)
		{
			if (!TemplateBoundsOverlay.TryGetIntactPlacementCells(map, terrainInfo, anchor, out var cells))
			{
				ClearCell(anchor);
				return;
			}

			foreach (var cell in cells)
				ClearCell(cell);
		}

		void SaveUndoCell(CPos cell)
		{
			undoTiles.Enqueue(new UndoTile(cell, map.Tiles[cell], map.Height[cell]));
		}

		void ClearCell(CPos cell)
		{
			if (!map.Tiles.Contains(cell))
				return;

			SaveUndoCell(cell);
			map.Tiles[cell] = defaultTile;
			map.Height[cell] = 0;
			map.CustomTerrain[cell] = byte.MaxValue;
		}

		void PaintTemplate(ushort template, TerrainTemplateInfo terrainTemplate, CPos cellToPaint, HashSet<CPos> skipUndo = null)
		{
			var mapTiles = map.Tiles;
			var mapHeight = map.Height;
			var baseHeight = mapHeight.Contains(cellToPaint) ? mapHeight[cellToPaint] : (byte)0;

			var i = 0;
			for (var y = 0; y < terrainTemplate.Size.Y; y++)
			{
				for (var x = 0; x < terrainTemplate.Size.X; x++, i++)
				{
					if (!terrainTemplate.Contains(i) || terrainTemplate[i] == null)
						continue;

					var index = terrainTemplate.PickAny
						? (byte)Game.CosmeticRandom.Next(0, terrainTemplate.TilesCount)
						: (byte)i;
					var c = cellToPaint + new CVec(x, y);
					if (!mapTiles.Contains(c))
						continue;

					if (skipUndo == null || !skipUndo.Contains(c))
						SaveUndoCell(c);

					mapTiles[c] = new TerrainTile(template, index);
					mapHeight[c] = (byte)(baseHeight + terrainTemplate[index].Height)
						.Clamp(0, map.Grid.MaximumTerrainHeight);
				}
			}
		}

		ushort PickTemplate()
		{
			if (templates.Length == 1)
				return templates[0];

			if (mixMode == EditorAssetMixMode.Sequential)
				return templates[nextTemplate++ % templates.Length];

			return templates[Game.CosmeticRandom.Next(templates.Length)];
		}
	}

	static class EditorTemplateRoadAlignment
	{
		public readonly struct Profile
		{
			public readonly float RoadCenterX;
			public readonly float RoadCenterY;
			public readonly int MinRoadX;
			public readonly int MaxRoadX;
			public readonly int MinRoadY;
			public readonly int MaxRoadY;
			public readonly bool HasRoad;

			public Profile(
				float roadCenterX,
				float roadCenterY,
				int minRoadX,
				int maxRoadX,
				int minRoadY,
				int maxRoadY,
				bool hasRoad)
			{
				RoadCenterX = roadCenterX;
				RoadCenterY = roadCenterY;
				MinRoadX = minRoadX;
				MaxRoadX = maxRoadX;
				MinRoadY = minRoadY;
				MaxRoadY = maxRoadY;
				HasRoad = hasRoad;
			}
		}

		public static Profile GetProfile(ITemplatedTerrainInfo terrainInfo, byte roadTerrainIndex, TerrainTemplateInfo template)
		{
			if (template.PickAny)
			{
				return new Profile(0.5f, 0.5f, 0, 0, 0, 0, false);
			}

			var minRoadX = int.MaxValue;
			var maxRoadX = int.MinValue;
			var minRoadY = int.MaxValue;
			var maxRoadY = int.MinValue;
			var hasRoad = false;
			var templateWidth = template.Size.X;

			for (var i = 0; i < templateWidth * template.Size.Y; i++)
			{
				if (!template.Contains(i) || template[i] == null)
					continue;

				if (template[i].TerrainType != roadTerrainIndex)
					continue;

				hasRoad = true;
				var localX = i % templateWidth;
				var localY = i / templateWidth;
				minRoadX = Math.Min(minRoadX, localX);
				maxRoadX = Math.Max(maxRoadX, localX);
				minRoadY = Math.Min(minRoadY, localY);
				maxRoadY = Math.Max(maxRoadY, localY);
			}

			if (!hasRoad)
			{
				var centerX = (template.Size.X - 1) / 2f;
				var centerY = (template.Size.Y - 1) / 2f;
				return new Profile(centerX, centerY, 0, template.Size.X - 1, 0, template.Size.Y - 1, false);
			}

			return new Profile(
				(minRoadX + maxRoadX) / 2f,
				(minRoadY + maxRoadY) / 2f,
				minRoadX,
				maxRoadX,
				minRoadY,
				maxRoadY,
				true);
		}
	}

	sealed record UndoTile(CPos Cell, TerrainTile MapTile, byte Height);

	static class EditorFillSelection
	{
		public static IEnumerable<CPos> SelectCells(IReadOnlyList<CPos> cells, int fillDensityPercent)
		{
			if (fillDensityPercent >= 100 || cells.Count == 0)
				return cells;

			var count = (cells.Count * fillDensityPercent + 50) / 100;
			if (count <= 0)
				return Array.Empty<CPos>();

			if (count >= cells.Count)
				return cells;

			var shuffled = cells.ToArray();
			for (var i = shuffled.Length - 1; i > 0; i--)
			{
				var j = Game.CosmeticRandom.Next(i + 1);
				(shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
			}

			return shuffled.Take(count);
		}
	}
}
