#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	[Desc("Required for shroud and fog visibility checks. Add this to the player actor.")]
	public class ShroudInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Shroud(init.Self); }
	}

	public class Shroud
	{
		[Sync] public bool Disabled;

		public event Action<IEnumerable<PPos>> CellsChanged;

		readonly Actor self;
		readonly Map map;

		readonly CellLayer<short> visibleCount;
		readonly CellLayer<short> generatedShroudCount;
		readonly CellLayer<bool> explored;

		// Cache of visibility that was added, so no matter what crazy trait code does, it
		// can't make us invalid.
		readonly Dictionary<Actor, PPos[]> visibility = new Dictionary<Actor, PPos[]>();
		readonly Dictionary<Actor, PPos[]> generation = new Dictionary<Actor, PPos[]>();

		public int Hash { get; private set; }

		static readonly Func<PPos, bool> TruthPredicate = _ => true;
		readonly Func<PPos, bool> shroudEdgeTest;
		readonly Func<PPos, bool> isExploredTest;
		readonly Func<PPos, bool> isVisibleTest;

		public Shroud(Actor self)
		{
			this.self = self;
			map = self.World.Map;

			visibleCount = new CellLayer<short>(map);
			generatedShroudCount = new CellLayer<short>(map);
			explored = new CellLayer<bool>(map);

			shroudEdgeTest = map.Contains;

			isExploredTest = IsExplored;
			isVisibleTest = IsVisible;
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
				if (!explored[uv])
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

		public void ExploreAll(World world)
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

			return map.ProjectedCellsCovering(uv).Any(isExploredTest);
		}

		public bool IsExplored(PPos puv)
		{
			if (!ShroudEnabled)
				return true;

			var uv = (MPos)puv;
			return explored.Contains(uv) && explored[uv] && (generatedShroudCount[uv] == 0 || visibleCount[uv] > 0);
		}

		public bool ShroudEnabled { get { return !Disabled && self.World.LobbyInfo.GlobalSettings.Shroud; } }

		/// <summary>
		/// Returns a fast exploration lookup that skips the usual validation.
		/// The return value should not be cached across ticks, and should not
		/// be called with cells outside the map bounds.
		/// </summary>
		public Func<PPos, bool> IsExploredTest
		{
			get
			{
				// If shroud isn't enabled, then we can see everything inside the map.
				if (!ShroudEnabled)
					return shroudEdgeTest;

				return isExploredTest;
			}
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

			return map.ProjectedCellsCovering(uv).Any(isVisibleTest);
		}

		// In internal shroud coords
		public bool IsVisible(PPos puv)
		{
			if (!FogEnabled)
				return true;

			var uv = (MPos)puv;
			return visibleCount.Contains(uv) && visibleCount[uv] > 0;
		}

		public bool FogEnabled { get { return !Disabled && self.World.LobbyInfo.GlobalSettings.Fog; } }

		/// <summary>
		/// Returns a fast visibility lookup that skips the usual validation.
		/// The return value should not be cached across ticks, and should not
		/// be called with cells outside the map bounds.
		/// </summary>
		public Func<PPos, bool> IsVisibleTest
		{
			get
			{
				// If fog isn't enabled, then we can see everything.
				if (!FogEnabled)
					return TruthPredicate;

				// If fog is enabled, we can use the fast test that just does the core check.
				return isVisibleTest;
			}
		}

		public bool Contains(PPos uv)
		{
			// Check that uv is inside the map area. There is nothing special
			// about explored here: any of the CellLayers would have been suitable.
			return explored.Contains((MPos)uv);
		}
	}
}
