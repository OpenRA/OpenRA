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

namespace OpenRA.Traits
{
	[Desc("Required for shroud and fog visibility checks. Add this to the player actor.")]
	public class ShroudInfo : TraitInfo, ILobbyOptions
	{
		[Desc("Descriptive label for the fog checkbox in the lobby.")]
		public readonly string FogCheckboxLabel = "Fog of War";

		[Desc("Tooltip description for the fog checkbox in the lobby.")]
		public readonly string FogCheckboxDescription = "Line of sight is required to view enemy forces";

		[Desc("Default value of the fog checkbox in the lobby.")]
		public readonly bool FogCheckboxEnabled = true;

		[Desc("Prevent the fog enabled state from being changed in the lobby.")]
		public readonly bool FogCheckboxLocked = false;

		[Desc("Whether to display the fog checkbox in the lobby.")]
		public readonly bool FogCheckboxVisible = true;

		[Desc("Display order for the fog checkbox in the lobby.")]
		public readonly int FogCheckboxDisplayOrder = 0;

		[Desc("Descriptive label for the explored map checkbox in the lobby.")]
		public readonly string ExploredMapCheckboxLabel = "Explored Map";

		[Desc("Tooltip description for the explored map checkbox in the lobby.")]
		public readonly string ExploredMapCheckboxDescription = "Initial map shroud is revealed";

		[Desc("Default value of the explore map checkbox in the lobby.")]
		public readonly bool ExploredMapCheckboxEnabled = false;

		[Desc("Prevent the explore map enabled state from being changed in the lobby.")]
		public readonly bool ExploredMapCheckboxLocked = false;

		[Desc("Whether to display the explore map checkbox in the lobby.")]
		public readonly bool ExploredMapCheckboxVisible = true;

		[Desc("Display order for the explore map checkbox in the lobby.")]
		public readonly int ExploredMapCheckboxDisplayOrder = 0;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("explored", ExploredMapCheckboxLabel, ExploredMapCheckboxDescription,
				ExploredMapCheckboxVisible, ExploredMapCheckboxDisplayOrder, ExploredMapCheckboxEnabled, ExploredMapCheckboxLocked);
			yield return new LobbyBooleanOption("fog", FogCheckboxLabel, FogCheckboxDescription,
				FogCheckboxVisible, FogCheckboxDisplayOrder, FogCheckboxEnabled, FogCheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return new Shroud(init.Self, this); }
	}

	public class Shroud : ISync, INotifyCreated, ITick
	{
		public enum SourceType : byte { PassiveVisibility, Shroud, Visibility }
		public event Action<PPos> OnShroudChanged;

		enum ShroudCellType : byte { Shroud, Fog, Visible }
		class ShroudSource
		{
			public readonly SourceType Type;
			public readonly PPos[] ProjectedCells;

			public ShroudSource(SourceType type, PPos[] projectedCells)
			{
				Type = type;
				ProjectedCells = projectedCells;
			}
		}

		readonly Actor self;
		readonly ShroudInfo info;
		readonly Map map;

		// Individual shroud modifier sources (type and area)
		readonly Dictionary<object, ShroudSource> sources = new Dictionary<object, ShroudSource>();

		// Per-cell count of each source type, used to resolve the final cell type
		readonly ProjectedCellLayer<short> passiveVisibleCount;
		readonly ProjectedCellLayer<short> visibleCount;
		readonly ProjectedCellLayer<short> generatedShroudCount;
		readonly ProjectedCellLayer<bool> explored;
		readonly ProjectedCellLayer<bool> touched;
		bool anyCellTouched;

		// Per-cell cache of the resolved cell type (shroud/fog/visible)
		readonly ProjectedCellLayer<ShroudCellType> resolvedType;

		[Sync]
		bool disabled;
		public bool Disabled
		{
			get
			{
				return disabled;
			}

			set
			{
				if (disabled == value)
					return;

				disabled = value;
			}
		}

		bool fogEnabled;
		public bool FogEnabled { get { return !Disabled && fogEnabled; } }
		public bool ExploreMapEnabled { get; private set; }

		public int Hash { get; private set; }

		// Enabled at runtime on first use
		bool shroudGenerationEnabled;
		bool passiveVisibilityEnabled;

		public Shroud(Actor self, ShroudInfo info)
		{
			this.self = self;
			this.info = info;
			map = self.World.Map;

			passiveVisibleCount = new ProjectedCellLayer<short>(map);
			visibleCount = new ProjectedCellLayer<short>(map);
			generatedShroudCount = new ProjectedCellLayer<short>(map);
			explored = new ProjectedCellLayer<bool>(map);
			touched = new ProjectedCellLayer<bool>(map);
			anyCellTouched = true;

			// Defaults to 0 = Shroud
			resolvedType = new ProjectedCellLayer<ShroudCellType>(map);
		}

		void INotifyCreated.Created(Actor self)
		{
			var gs = self.World.LobbyInfo.GlobalSettings;
			fogEnabled = gs.OptionOrDefault("fog", info.FogCheckboxEnabled);

			ExploreMapEnabled = gs.OptionOrDefault("explored", info.ExploredMapCheckboxEnabled);
			if (ExploreMapEnabled)
				self.World.AddFrameEndTask(w => ExploreAll());
		}

		void ITick.Tick(Actor self)
		{
			if (!anyCellTouched)
				return;

			anyCellTouched = false;

			if (OnShroudChanged == null)
				return;

			foreach (var puv in map.ProjectedCells)
			{
				var index = touched.Index(puv);
				if (!touched[index])
					continue;

				touched[index] = false;

				var type = ShroudCellType.Shroud;

				if (explored[index])
				{
					var count = visibleCount[index];
					if (!shroudGenerationEnabled || count > 0 || generatedShroudCount[index] == 0)
					{
						if (passiveVisibilityEnabled)
							count += passiveVisibleCount[index];

						type = count > 0 ? ShroudCellType.Visible : ShroudCellType.Fog;
					}
				}

				var oldResolvedType = resolvedType[index];
				if (type != oldResolvedType)
				{
					resolvedType[index] = type;
					OnShroudChanged(puv);
				}
			}

			Hash = Sync.HashPlayer(self.Owner) + self.World.WorldTick;
		}

		public static IEnumerable<PPos> ProjectedCellsInRange(Map map, WPos pos, WDist minRange, WDist maxRange, int maxHeightDelta = -1)
		{
			// Account for potential extra half-cell from odd-height terrain
			var r = (maxRange.Length + 1023 + 512) / 1024;
			var minLimit = minRange.LengthSquared;
			var maxLimit = maxRange.LengthSquared;

			// Project actor position into the shroud plane
			var projectedPos = pos - new WVec(0, pos.Z, pos.Z);
			var projectedCell = map.CellContaining(projectedPos);
			var projectedHeight = pos.Z / 512;

			foreach (var c in map.FindTilesInAnnulus(projectedCell, minRange.Length / 1024, r, true))
			{
				var dist = (map.CenterOfCell(c) - projectedPos).HorizontalLengthSquared;
				if (dist <= maxLimit && (dist == 0 || dist > minLimit))
				{
					var puv = (PPos)c.ToMPos(map);
					if (maxHeightDelta < 0 || map.ProjectedHeight(puv) < projectedHeight + maxHeightDelta)
						yield return puv;
				}
			}
		}

		public static IEnumerable<PPos> ProjectedCellsInRange(Map map, CPos cell, WDist range, int maxHeightDelta = -1)
		{
			return ProjectedCellsInRange(map, map.CenterOfCell(cell), WDist.Zero, range, maxHeightDelta);
		}

		public void AddSource(object key, SourceType type, PPos[] projectedCells)
		{
			if (sources.ContainsKey(key))
				throw new InvalidOperationException("Attempting to add duplicate shroud source");

			sources[key] = new ShroudSource(type, projectedCells);

			foreach (var puv in projectedCells)
			{
				// Force cells outside the visible bounds invisible
				if (!map.Contains(puv))
					continue;

				var index = touched.Index(puv);
				touched[index] = true;
				anyCellTouched = true;
				switch (type)
				{
					case SourceType.PassiveVisibility:
						passiveVisibilityEnabled = true;
						passiveVisibleCount[index]++;
						explored[index] = true;
						break;
					case SourceType.Visibility:
						visibleCount[index]++;
						explored[index] = true;
						break;
					case SourceType.Shroud:
						shroudGenerationEnabled = true;
						generatedShroudCount[index]++;
						break;
				}
			}
		}

		public void RemoveSource(object key)
		{
			if (!sources.TryGetValue(key, out var state))
				return;

			foreach (var puv in state.ProjectedCells)
			{
				// Cells outside the visible bounds don't increment visibleCount
				if (map.Contains(puv))
				{
					var index = touched.Index(puv);
					touched[index] = true;
					anyCellTouched = true;
					switch (state.Type)
					{
						case SourceType.PassiveVisibility:
							passiveVisibleCount[index]--;
							break;
						case SourceType.Visibility:
							visibleCount[index]--;
							break;
						case SourceType.Shroud:
							generatedShroudCount[index]--;
							break;
					}
				}
			}

			sources.Remove(key);
		}

		public void ExploreProjectedCells(World world, IEnumerable<PPos> cells)
		{
			foreach (var puv in cells)
			{
				if (map.Contains(puv))
				{
					var index = touched.Index(puv);
					if (!explored[index])
					{
						touched[index] = true;
						anyCellTouched = true;
						explored[index] = true;
					}
				}
			}
		}

		public void Explore(Shroud s)
		{
			if (map.Bounds != s.map.Bounds)
				throw new ArgumentException("The map bounds of these shrouds do not match.", "s");

			foreach (var puv in map.ProjectedCells)
			{
				var index = touched.Index(puv);
				if (!explored[index] && s.explored[index])
				{
					touched[index] = true;
					anyCellTouched = true;
					explored[index] = true;
				}
			}
		}

		public void ExploreAll()
		{
			foreach (var puv in map.ProjectedCells)
			{
				var index = touched.Index(puv);
				if (!explored[index])
				{
					touched[index] = true;
					anyCellTouched = true;
					explored[index] = true;
				}
			}
		}

		public void ResetExploration()
		{
			foreach (var puv in map.ProjectedCells)
			{
				var index = touched.Index(puv);
				touched[index] = true;
				explored[index] = (visibleCount[index] + passiveVisibleCount[index]) > 0;
			}

			anyCellTouched = true;
		}

		public bool IsExplored(WPos pos)
		{
			return IsExplored(map.ProjectedCellCovering(pos));
		}

		public bool IsExplored(CPos cell)
		{
			return IsExplored(cell.ToMPos(map));
		}

		public bool IsExplored(MPos uv)
		{
			if (!map.Contains(uv))
				return false;

			foreach (var puv in map.ProjectedCellsCovering(uv))
				if (IsExplored(puv))
					return true;

			return false;
		}

		public bool IsExplored(PPos puv)
		{
			if (Disabled)
				return map.Contains(puv);

			return resolvedType.Contains(puv) && resolvedType[puv] > ShroudCellType.Shroud;
		}

		public bool IsVisible(WPos pos)
		{
			return IsVisible(map.ProjectedCellCovering(pos));
		}

		public bool IsVisible(CPos cell)
		{
			return IsVisible(cell.ToMPos(map));
		}

		public bool IsVisible(MPos uv)
		{
			foreach (var puv in map.ProjectedCellsCovering(uv))
				if (IsVisible(puv))
					return true;

			return false;
		}

		// In internal shroud coords
		public bool IsVisible(PPos puv)
		{
			if (!FogEnabled)
				return map.Contains(puv);

			return resolvedType.Contains(puv) && resolvedType[puv] == ShroudCellType.Visible;
		}

		public bool Contains(PPos uv)
		{
			// Check that uv is inside the map area. There is nothing special
			// about explored here: any of the CellLayers would have been suitable.
			return explored.Contains(uv);
		}
	}
}
