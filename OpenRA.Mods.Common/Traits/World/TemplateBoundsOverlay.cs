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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[Desc("Editor overlay that draws borders around placed terrain templates.")]
	public class TemplateBoundsOverlayInfo : TraitInfo<TemplateBoundsOverlay> { }

	public class TemplateBoundsOverlay : IRenderAnnotations
	{
		public bool Enabled;

		static readonly Color PlacedTileColor = Color.Yellow;
		static readonly Color ObscuredTileColor = Color.FromArgb(255, 255, 128, 0);

		readonly struct PlacementKey
		{
			public readonly ushort TemplateType;
			public readonly CPos Key;

			public PlacementKey(ushort templateType, CPos key)
			{
				TemplateType = templateType;
				Key = key;
			}
		}

		/// <summary>
		/// Returns true for terrain that should be highlighted as an editor-placed tile
		/// (excludes default terrain and background clear fill).
		/// </summary>
		public static bool IsEditorPlacedTile(Map map, ITemplatedTerrainInfo terrainInfo, CPos cell)
		{
			if (!map.Contains(cell))
				return false;

			var tile = map.Tiles[cell];
			if (tile.Type == terrainInfo.DefaultTerrainTile.Type && tile.Index == terrainInfo.DefaultTerrainTile.Index)
				return false;

			if (!terrainInfo.Templates.TryGetValue(tile.Type, out var template))
				return false;

			return !IsBackgroundFill(terrainInfo, template);
		}

		static bool IsBackgroundFill(ITemplatedTerrainInfo terrainInfo, TerrainTemplateInfo template)
		{
			if (!template.PickAny || template.Size.X != 1 || template.Size.Y != 1)
				return false;

			for (var i = 0; i < template.TilesCount; i++)
			{
				if (!template.Contains(i) || template[i] == null)
					continue;

				if (terrainInfo.TerrainTypes[template[i].TerrainType].Type != "Clear")
					return false;
			}

			return true;
		}

		public static CPos[] BuildMatchingTemplateRegion(
			TerrainTemplateInfo template,
			CPos anchor,
			ushort templateType,
			Map map)
		{
			var templateWidth = template.Size.X;
			var cells = new List<CPos>();
			for (byte i = 0; i < templateWidth * template.Size.Y; i++)
			{
				if (!template.Contains(i) || template[i] == null)
					continue;

				var cell = anchor + new CVec(i % templateWidth, i / templateWidth);
				if (!map.Tiles.Contains(cell))
					continue;

				var tile = map.Tiles[cell];
				if (tile.Type == templateType && tile.Index == i)
					cells.Add(cell);
			}

			return cells.ToArray();
		}

		public static CPos[] BuildMissingTemplateSlots(
			TerrainTemplateInfo template,
			CPos anchor,
			ushort templateType,
			Map map)
		{
			var templateWidth = template.Size.X;
			var cells = new List<CPos>();
			for (byte i = 0; i < templateWidth * template.Size.Y; i++)
			{
				if (!template.Contains(i) || template[i] == null)
					continue;

				var cell = anchor + new CVec(i % templateWidth, i / templateWidth);
				if (!map.Tiles.Contains(cell))
				{
					cells.Add(cell);
					continue;
				}

				var tile = map.Tiles[cell];
				if (tile.Type != templateType || tile.Index != i)
					cells.Add(cell);
			}

			return cells.ToArray();
		}

		public static CPos[] BuildTemplateFootprintCells(TerrainTemplateInfo template, CPos anchor)
		{
			var templateWidth = template.Size.X;
			var cells = new List<CPos>();
			for (byte i = 0; i < templateWidth * template.Size.Y; i++)
			{
				if (!template.Contains(i) || template[i] == null)
					continue;

				cells.Add(anchor + new CVec(i % templateWidth, i / templateWidth));
			}

			return cells.ToArray();
		}

		/// <summary>
		/// Describes one placed template instance for editor selection preview.
		/// </summary>
		public static bool TryGetPlacementContext(
			Map map,
			ITemplatedTerrainInfo terrainInfo,
			CPos cell,
			out ushort templateType,
			out CPos anchor,
			out CPos[] matchingCells,
			out CPos[] missingCells)
		{
			templateType = 0;
			anchor = cell;
			matchingCells = null;
			missingCells = null;

			if (!TryGetAnchor(map, terrainInfo, cell, out var template, out anchor, out templateType))
				return false;

			if (template.PickAny || template.Size.X == 1 && template.Size.Y == 1)
			{
				matchingCells = [cell];
				missingCells = [];
				return true;
			}

			matchingCells = BuildMatchingTemplateRegion(template, anchor, templateType, map);
			missingCells = BuildMissingTemplateSlots(template, anchor, templateType, map);
			if (matchingCells.Length == 0 || !Contains(matchingCells, cell))
				matchingCells = [cell];

			return true;
		}

		/// <summary>
		/// Returns the cells that form one intact editor placement (matches the Tiles overlay outline).
		/// </summary>
		public static bool TryGetIntactPlacementCells(
			Map map,
			ITemplatedTerrainInfo terrainInfo,
			CPos cell,
			out CPos[] cells)
		{
			cells = null;
			if (!TryGetAnchor(map, terrainInfo, cell, out var template, out var anchor, out var templateType))
				return false;

			if (template.PickAny || template.Size.X == 1 && template.Size.Y == 1)
				cells = [cell];
			else
			{
				cells = BuildMatchingTemplateRegion(template, anchor, templateType, map);
				if (cells.Length == 0 || !Contains(cells, cell))
					cells = [cell];
			}

			return cells.Length > 0;
		}

		static bool TryGetAnchor(
			Map map,
			ITemplatedTerrainInfo terrainInfo,
			CPos cell,
			out TerrainTemplateInfo template,
			out CPos anchor,
			out ushort templateType)
		{
			template = null;
			anchor = cell;
			templateType = 0;

			if (!IsEditorPlacedTile(map, terrainInfo, cell))
				return false;

			var tile = map.Tiles[cell];
			templateType = tile.Type;
			if (!terrainInfo.Templates.TryGetValue(tile.Type, out template))
				return false;

			if (template.PickAny || (template.Size.X == 1 && template.Size.Y == 1))
				return true;

			if (!template.Contains(tile.Index) || template[tile.Index] == null)
				return true;

			var templateWidth = template.Size.X;
			var localX = tile.Index % templateWidth;
			var localY = tile.Index / templateWidth;
			anchor = cell - new CVec(localX, localY);
			return true;
		}

		IEnumerable<IRenderable> IRenderAnnotations.RenderAnnotations(Actor self, WorldRenderer wr)
		{
			if (!Enabled)
				yield break;

			var map = wr.World.Map;
			if (map.Rules.TerrainInfo is not ITemplatedTerrainInfo terrainInfo)
				yield break;

			var processedPlacements = new HashSet<PlacementKey>();
			var processedObscuredCells = new HashSet<CPos>();
			var obscuredRegions = new List<CPos[]>();

			foreach (var uv in wr.Viewport.AllVisibleCells.CandidateMapCoords)
			{
				var cell = uv.ToCPos(map);
				if (!TryGetIntactPlacementCells(map, terrainInfo, cell, out var region))
					continue;

				if (!TryGetAnchor(map, terrainInfo, cell, out var template, out var anchor, out var templateType))
					continue;

				PlacementKey placementKey;
				if (template.PickAny || template.Size.X == 1 && template.Size.Y == 1 || region.Length == 1)
					placementKey = new PlacementKey(templateType, cell);
				else
					placementKey = new PlacementKey(templateType, anchor);

				if (!processedPlacements.Add(placementKey))
					continue;

				yield return new BorderedRegionRenderable(region, PlacedTileColor, 1, Color.Black, 0);

				if (template.PickAny || template.Size.X == 1 && template.Size.Y == 1)
					continue;

				var obscured = BuildMissingTemplateSlots(template, anchor, templateType, map);
				if (obscured.Length == 0)
					continue;

				var batch = new List<CPos>();
				foreach (var obscuredCell in obscured)
				{
					if (processedObscuredCells.Add(obscuredCell))
						batch.Add(obscuredCell);
				}

				if (batch.Count > 0)
					obscuredRegions.Add(batch.ToArray());
			}

			foreach (var region in obscuredRegions)
				yield return new BorderedRegionRenderable(region, ObscuredTileColor, 1, Color.Black, 0, true);
		}

		static bool Contains(CPos[] region, CPos cell)
		{
			foreach (var c in region)
			{
				if (c == cell)
					return true;
			}

			return false;
		}

		bool IRenderAnnotations.SpatiallyPartitionable => false;
	}
}
