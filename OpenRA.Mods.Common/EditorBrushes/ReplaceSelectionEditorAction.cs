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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Widgets;

namespace OpenRA.Mods.Common.EditorBrushes
{
	public enum EditorReplaceLayer
	{
		Tile,
		Resources,
		Actors
	}

	public sealed class TileReplacePlan
	{
		public List<CPos> IntactAnchors { get; } = [];
		public HashSet<CPos> PackCells { get; } = [];
	}

	sealed class ReplaceSelectionEditorAction : IEditorAction
	{
		[FluentReference]
		const string ReplacedSelection = "notification-replaced-selection";

		public string Text { get; }

		readonly DeleteAreaAction clearLayerAction;
		readonly IEditorAction fillAction;

		ReplaceSelectionEditorAction(string text, DeleteAreaAction clearLayerAction, IEditorAction fillAction)
		{
			Text = text;
			this.clearLayerAction = clearLayerAction;
			this.fillAction = fillAction;
		}

		public void Execute()
		{
			Do();
		}

		public void Do()
		{
			clearLayerAction?.Do();
			fillAction?.Do();
		}

		public void Undo()
		{
			fillAction?.Undo();
			clearLayerAction?.Undo();
		}

		public static ReplaceSelectionEditorAction Create(
			MapBlitFilters replaceFilters,
			EditorReplaceLayer withLayer,
			ushort[] tileTemplates,
			IEnumerable<string> resourceTypes,
			IEnumerable<ActorReference> actorReferences,
			EditorAssetMixMode mixMode,
			bool includeEmptySpaces,
			Map map,
			CellCoordsRegion area,
			IReadOnlySet<CPos> selectionMask,
			IResourceLayer resourceLayer,
			EditorActorLayer editorActorLayer)
		{
			if (withLayer == EditorReplaceLayer.Tile && !includeEmptySpaces)
			{
				var plan = BuildTileReplacePlan(
					map, resourceLayer, editorActorLayer, area, selectionMask, replaceFilters);
				var clearMask = ComputeFillMask(
					map, resourceLayer, editorActorLayer, area, selectionMask, replaceFilters, false);

				IEditorAction fill = plan.IntactAnchors.Count > 0 || plan.PackCells.Count > 0
					? new ReplaceTemplatePlacementsEditorAction(tileTemplates, mixMode, map, plan)
					: null;

				DeleteAreaAction clear = null;
				if (replaceFilters != MapBlitFilters.None && clearMask.Count > 0)
				{
					clear = new DeleteAreaAction(
						map, replaceFilters, area, clearMask, resourceLayer, editorActorLayer);
				}

				if (fill == null && clear == null)
					return null;

				var text = FluentProvider.GetMessage(ReplacedSelection);
				return new ReplaceSelectionEditorAction(text, clear, fill);
			}

			var fillMask = ComputeFillMask(
				map, resourceLayer, editorActorLayer, area, selectionMask, replaceFilters, includeEmptySpaces);
			if (fillMask.Count == 0)
				return null;

			DeleteAreaAction clearAll = null;
			if (replaceFilters != MapBlitFilters.None)
			{
				clearAll = new DeleteAreaAction(
					map, replaceFilters, area, fillMask, resourceLayer, editorActorLayer);
			}

			IEditorAction fillAll = withLayer switch
			{
				EditorReplaceLayer.Tile => new FillSelectionWithTileEditorAction(
					tileTemplates, mixMode, 100, map, area, null, null),
				EditorReplaceLayer.Resources when resourceLayer != null => new FillSelectionWithResourceEditorAction(
					resourceLayer, resourceTypes, mixMode, 100, EditorFillMode.Overlap, area, fillMask),
				EditorReplaceLayer.Actors => new FillSelectionWithActorEditorAction(
					editorActorLayer, actorReferences, mixMode, 100, map, area, fillMask),
				_ => null
			};

			if (fillAll == null)
				return null;

			var message = FluentProvider.GetMessage(ReplacedSelection);
			return new ReplaceSelectionEditorAction(message, clearAll, fillAll);
		}

		static TileReplacePlan BuildTileReplacePlan(
			Map map,
			IResourceLayer resourceLayer,
			EditorActorLayer editorActorLayer,
			CellCoordsRegion area,
			IReadOnlySet<CPos> selectionMask,
			MapBlitFilters replaceFilters)
		{
			var plan = new TileReplacePlan();

			if (replaceFilters.HasFlag(MapBlitFilters.Terrain)
				&& map.Rules.TerrainInfo is ITemplatedTerrainInfo terrainInfo)
			{
				foreach (var anchor in TemplateBoundsOverlay.EnumeratePlacedTemplateAnchors(
					map, terrainInfo, area, selectionMask))
				{
					if (!TemplateBoundsOverlay.TryGetIntactPlacementCells(map, terrainInfo, anchor, out var cells))
						continue;

					if (!TemplateBoundsOverlay.TryGetPlacementContext(
						map, terrainInfo, anchor, out var templateType, out _, out var matchingCells, out _))
						continue;

					if (!terrainInfo.Templates.TryGetValue(templateType, out var template))
						continue;

					var isMultiCellIntact = !(template.PickAny || template.Size.X == 1 && template.Size.Y == 1)
						&& matchingCells.Length > 1;

					if (isMultiCellIntact)
						plan.IntactAnchors.Add(anchor);
					else
					{
						foreach (var cell in cells)
							plan.PackCells.Add(cell);
					}
				}
			}

			if (replaceFilters.HasFlag(MapBlitFilters.Actors))
			{
				foreach (var preview in editorActorLayer.PreviewsInCellRegion(area))
				{
					if (selectionMask != null && !selectionMask.Contains(preview.Location))
						continue;

					plan.PackCells.Add(preview.Location);
				}
			}

			if (replaceFilters.HasFlag(MapBlitFilters.Resources) && resourceLayer != null)
			{
				IEnumerable<CPos> selectionCells = selectionMask != null ? selectionMask : area.ToList();
				foreach (var cell in selectionCells)
				{
					if (!map.Tiles.Contains(cell))
						continue;

					var resource = resourceLayer.GetResource(cell);
					if (!string.IsNullOrEmpty(resource.Type))
						plan.PackCells.Add(cell);
				}
			}

			return plan;
		}

		public static bool HasReplaceableContent(
			MapBlitFilters replaceFilters,
			EditorReplaceLayer withLayer,
			bool includeEmptySpaces,
			Map map,
			CellCoordsRegion area,
			IReadOnlySet<CPos> selectionMask,
			IResourceLayer resourceLayer,
			EditorActorLayer editorActorLayer)
		{
			if (withLayer == EditorReplaceLayer.Tile && !includeEmptySpaces)
			{
				var plan = BuildTileReplacePlan(
					map, resourceLayer, editorActorLayer, area, selectionMask, replaceFilters);
				return plan.IntactAnchors.Count > 0 || plan.PackCells.Count > 0;
			}

			return ComputeFillMask(
				map, resourceLayer, editorActorLayer, area, selectionMask, replaceFilters, includeEmptySpaces).Count > 0;
		}

		public static HashSet<CPos> ComputeFillMask(
			Map map,
			IResourceLayer resourceLayer,
			EditorActorLayer editorActorLayer,
			CellCoordsRegion area,
			IReadOnlySet<CPos> selectionMask,
			MapBlitFilters replaceFilters,
			bool includeEmptySpaces)
		{
			var fillMask = new HashSet<CPos>();
			if (replaceFilters == MapBlitFilters.None)
				return fillMask;

			IEnumerable<CPos> selectionCells = selectionMask != null ? selectionMask : area.ToList();

			if (replaceFilters.HasFlag(MapBlitFilters.Terrain))
			{
				foreach (var cell in selectionCells)
				{
					if (!map.Tiles.Contains(cell))
						continue;

					if (includeEmptySpaces || HasReplaceableTerrain(map, cell))
						fillMask.Add(cell);
				}
			}

			if (replaceFilters.HasFlag(MapBlitFilters.Resources) && resourceLayer != null)
			{
				foreach (var cell in selectionCells)
				{
					if (!map.Tiles.Contains(cell))
						continue;

					var resource = resourceLayer.GetResource(cell);
					if (!string.IsNullOrEmpty(resource.Type))
						fillMask.Add(cell);
				}
			}

			if (replaceFilters.HasFlag(MapBlitFilters.Actors))
			{
				foreach (var preview in editorActorLayer.PreviewsInCellRegion(area))
				{
					foreach (var cell in preview.Footprint.Keys)
					{
						if (!map.Tiles.Contains(cell))
							continue;

						if (selectionMask != null && !selectionMask.Contains(cell))
							continue;

						fillMask.Add(cell);
					}
				}
			}

			return fillMask;
		}

		static bool HasReplaceableTerrain(Map map, CPos cell)
		{
			return map.GetTerrainInfo(cell).Type != "Clear";
		}
	}
}
