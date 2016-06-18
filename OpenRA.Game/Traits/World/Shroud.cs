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
using System.Linq;
using OpenRA.Network;

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
		public event Action<IEnumerable<PPos>> CellsChanged;

		readonly Actor self;
		readonly ShroudInfo info;
		readonly Map map;

		readonly CellLayer<short> visibleCount;
		readonly CellLayer<short> generatedShroudCount;
		readonly CellLayer<bool> explored;

		// Cache of visibility that was added, so no matter what crazy trait code does, it
		// can't make us invalid.
		readonly Dictionary<Actor, PPos[]> visibility = new Dictionary<Actor, PPos[]>();
		readonly Dictionary<Actor, PPos[]> generation = new Dictionary<Actor, PPos[]>();

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

		public Shroud(Actor self, ShroudInfo info)
		{
			this.self = self;
			this.info = info;
			map = self.World.Map;

			visibleCount = new CellLayer<short>(map);
			generatedShroudCount = new CellLayer<short>(map);
			explored = new CellLayer<bool>(map);
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

		public void AddProjectedVisibility(Actor a, PPos[] visible)
		{
			if (!a.Owner.IsAlliedWith(self.Owner))
				return;

			foreach (var puv in visible)
			{
				// Force cells outside the visible bounds invisible
				if (!map.Contains(puv))
					continue;

				var uv = (MPos)puv;
				visibleCount[uv]++;
				explored[uv] = true;
			}

			if (visibility.ContainsKey(a))
				throw new InvalidOperationException("Attempting to add duplicate actor visibility");

			visibility[a] = visible;
			Invalidate(visible);
		}

		public void RemoveVisibility(Actor a)
		{
			PPos[] visible;
			if (!visibility.TryGetValue(a, out visible))
				return;

			foreach (var puv in visible)
			{
				// Cells outside the visible bounds don't increment visibleCount
				if (map.Contains(puv))
					visibleCount[(MPos)puv]--;
			}

			visibility.Remove(a);
			Invalidate(visible);
		}

		public void AddProjectedShroudGeneration(Actor a, PPos[] shrouded)
		{
			if (a.Owner.IsAlliedWith(self.Owner))
				return;

			foreach (var uv in shrouded)
				generatedShroudCount[(MPos)uv]++;

			if (generation.ContainsKey(a))
				throw new InvalidOperationException("Attempting to add duplicate shroud generation");

			generation[a] = shrouded;
			Invalidate(shrouded);
		}

		public void RemoveShroudGeneration(Actor a)
		{
			PPos[] shrouded;
			if (!generation.TryGetValue(a, out shrouded))
				return;

			foreach (var uv in shrouded)
				generatedShroudCount[(MPos)uv]--;

			generation.Remove(a);
			Invalidate(shrouded);
		}

		public void UpdatePlayerStance(World w, Player player, Stance oldStance, Stance newStance)
		{
			if (oldStance == newStance)
				return;

			foreach (var a in w.Actors.Where(a => a.Owner == player))
			{
				PPos[] visible = null;
				PPos[] shrouded = null;
				foreach (var p in self.World.Players)
				{
					if (p.Shroud.visibility.TryGetValue(self, out visible))
					{
						p.Shroud.RemoveVisibility(self);
						p.Shroud.AddProjectedVisibility(self, visible);
					}

					if (p.Shroud.generation.TryGetValue(self, out shrouded))
					{
						p.Shroud.RemoveShroudGeneration(self);
						p.Shroud.AddProjectedShroudGeneration(self, shrouded);
					}
				}
			}
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
				var visible = visibleCount[uv] > 0;
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

			var uv = (MPos)puv;
			return explored.Contains(uv) && explored[uv] && (generatedShroudCount[uv] == 0 || visibleCount[uv] > 0);
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
			if (!visibleCount.Contains(uv))
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
			return visibleCount.Contains(uv) && visibleCount[uv] > 0;
		}

		public bool Contains(PPos uv)
		{
			// Check that uv is inside the map area. There is nothing special
			// about explored here: any of the CellLayers would have been suitable.
			return explored.Contains((MPos)uv);
		}
	}
}
