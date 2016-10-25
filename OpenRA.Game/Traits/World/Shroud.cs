#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
	public class ShroudInfo : ITraitInfo, ILobbyOptions
	{
		[Desc("Default value of the fog checkbox in the lobby.")]
		public bool FogEnabled = true;

		[Desc("Prevent the fog enabled state from being changed in the lobby.")]
		public bool FogLocked = false;

		[Desc("Default value of the explore map checkbox in the lobby.")]
		public bool ExploredMapEnabled = false;

		[Desc("Prevent the explore map enabled state from being changed in the lobby.")]
		public bool ExploredMapLocked = false;

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(Ruleset rules)
		{
			yield return new LobbyBooleanOption("explored", "Explored Map", ExploredMapEnabled, ExploredMapLocked);
			yield return new LobbyBooleanOption("fog", "Fog of War", FogEnabled, FogLocked);
		}

		public object Create(ActorInitializer init) { return new Shroud(init.Self, this); }
	}

	public class Shroud : ISync, INotifyCreated
	{
		public enum SourceType : byte { PassiveVisibility, Shroud, Visibility }
		public event Action<IEnumerable<PPos>> CellsChanged;

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
		readonly CellLayer<short> passiveVisibleCount;
		readonly CellLayer<short> visibleCount;
		readonly CellLayer<short> generatedShroudCount;
		readonly CellLayer<bool> explored;

		// Per-cell cache of the resolved cell type (shroud/fog/visible)
		readonly CellLayer<ShroudCellType> resolvedType;

		[Sync] bool disabled;
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
				Invalidate(map.ProjectedCellBounds);
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

			passiveVisibleCount = new CellLayer<short>(map);
			visibleCount = new CellLayer<short>(map);
			generatedShroudCount = new CellLayer<short>(map);
			explored = new CellLayer<bool>(map);

			// Defaults to 0 = Shroud
			resolvedType = new CellLayer<ShroudCellType>(map);
		}

		void INotifyCreated.Created(Actor self)
		{
			var gs = self.World.LobbyInfo.GlobalSettings;
			fogEnabled = gs.OptionOrDefault("fog", info.FogEnabled);

			ExploreMapEnabled = gs.OptionOrDefault("explored", info.ExploredMapEnabled);
			if (ExploreMapEnabled)
				self.World.AddFrameEndTask(w => ExploreAll());
		}

		void Invalidate(IEnumerable<PPos> changed)
		{
			foreach (var puv in changed)
			{
				var uv = (MPos)puv;
				var type = ShroudCellType.Shroud;

				if (explored[uv] && (!shroudGenerationEnabled || generatedShroudCount[uv] == 0 || visibleCount[uv] > 0))
				{
					var count = visibleCount[uv];
					if (passiveVisibilityEnabled)
						count += passiveVisibleCount[uv];

					type = count > 0 ? ShroudCellType.Visible : ShroudCellType.Fog;
				}

				resolvedType[uv] = type;
			}

			if (CellsChanged != null)
				CellsChanged(changed);

			var oldHash = Hash;
			Hash = Sync.HashPlayer(self.Owner) + self.World.WorldTick * 3;

			// Invalidate may be called multiple times in one world tick, which is decoupled from rendering.
			if (oldHash == Hash)
				Hash += 1;
		}

		public static IEnumerable<PPos> ProjectedCellsInRange(Map map, WPos pos, WDist range)
		{
			// Account for potential extra half-cell from odd-height terrain
			var r = (range.Length + 1023 + 512) / 1024;
			var limit = range.LengthSquared;

			// Project actor position into the shroud plane
			var projectedPos = pos - new WVec(0, pos.Z, pos.Z);
			var projectedCell = map.CellContaining(projectedPos);

			foreach (var c in map.FindTilesInCircle(projectedCell, r, true))
				if ((map.CenterOfCell(c) - projectedPos).HorizontalLengthSquared <= limit)
					yield return (PPos)c.ToMPos(map);
		}

		public static IEnumerable<PPos> ProjectedCellsInRange(Map map, CPos cell, WDist range)
		{
			return ProjectedCellsInRange(map, map.CenterOfCell(cell), range);
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

				var uv = (MPos)puv;
				switch (type)
				{
					case SourceType.PassiveVisibility:
						passiveVisibilityEnabled = true;
						passiveVisibleCount[uv]++;
						explored[uv] = true;
						break;
					case SourceType.Visibility:
						visibleCount[uv]++;
						explored[uv] = true;
						break;
					case SourceType.Shroud:
						shroudGenerationEnabled = true;
						generatedShroudCount[uv]++;
						break;
				}
			}

			Invalidate(projectedCells);
		}

		public void RemoveSource(object key)
		{
			ShroudSource state;
			if (!sources.TryGetValue(key, out state))
				return;

			foreach (var puv in state.ProjectedCells)
			{
				// Cells outside the visible bounds don't increment visibleCount
				if (map.Contains(puv))
				{
					var uv = (MPos)puv;
					switch (state.Type)
					{
						case SourceType.PassiveVisibility:
							passiveVisibleCount[uv]--;
							break;
						case SourceType.Visibility:
							visibleCount[uv]--;
							break;
						case SourceType.Shroud:
							generatedShroudCount[uv]--;
							break;
					}
				}
			}

			sources.Remove(key);
			Invalidate(state.ProjectedCells);
		}

		public void ExploreProjectedCells(World world, IEnumerable<PPos> cells)
		{
			var changed = new HashSet<PPos>();
			foreach (var puv in cells)
			{
				var uv = (MPos)puv;
				if (map.Contains(puv) && !explored[uv])
				{
					explored[uv] = true;
					changed.Add(puv);
				}
			}

			Invalidate(changed);
		}

		public void Explore(Shroud s)
		{
			if (map.Bounds != s.map.Bounds)
				throw new ArgumentException("The map bounds of these shrouds do not match.", "s");

			var changed = new List<PPos>();
			foreach (var puv in map.ProjectedCellBounds)
			{
				var uv = (MPos)puv;
				if (!explored[uv] && s.explored[uv])
				{
					explored[uv] = true;
					changed.Add(puv);
				}
			}

			Invalidate(changed);
		}

		public void ExploreAll()
		{
			var changed = new List<PPos>();
			foreach (var puv in map.ProjectedCellBounds)
			{
				var uv = (MPos)puv;
				if (!explored[uv])
				{
					explored[uv] = true;
					changed.Add(puv);
				}
			}

			Invalidate(changed);
		}

		public void ResetExploration()
		{
			var changed = new List<PPos>();
			foreach (var puv in map.ProjectedCellBounds)
			{
				var uv = (MPos)puv;
				var visible = visibleCount[uv] + passiveVisibleCount[uv] > 0;
				if (explored[uv] != visible)
				{
					explored[uv] = visible;
					changed.Add(puv);
				}
			}

			Invalidate(changed);
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

			return resolvedType[(MPos)puv] > ShroudCellType.Shroud;
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
			if (!resolvedType.Contains(uv))
				return false;

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

			var uv = (MPos)puv;
			return resolvedType.Contains(uv) && resolvedType[uv] == ShroudCellType.Visible;
		}

		public bool Contains(PPos uv)
		{
			// Check that uv is inside the map area. There is nothing special
			// about explored here: any of the CellLayers would have been suitable.
			return explored.Contains((MPos)uv);
		}
	}
}
