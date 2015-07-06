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

		public event Action<IEnumerable<CPos>> CellsChanged;

		readonly Actor self;
		readonly Map map;

		readonly CellLayer<short> visibleCount;
		readonly CellLayer<short> generatedShroudCount;
		readonly CellLayer<bool> explored;

		// Cache of visibility that was added, so no matter what crazy trait code does, it
		// can't make us invalid.
		readonly Dictionary<Actor, CPos[]> visibility = new Dictionary<Actor, CPos[]>();
		readonly Dictionary<Actor, CPos[]> generation = new Dictionary<Actor, CPos[]>();

		public int Hash { get; private set; }

		static readonly Func<MPos, bool> TruthPredicate = _ => true;
		readonly Func<MPos, bool> shroudEdgeTest;
		readonly Func<MPos, bool> isExploredTest;
		readonly Func<MPos, bool> isVisibleTest;

		public Shroud(Actor self)
		{
			this.self = self;
			map = self.World.Map;

			visibleCount = new CellLayer<short>(map);
			generatedShroudCount = new CellLayer<short>(map);
			explored = new CellLayer<bool>(map);

			shroudEdgeTest = map.Contains;
			isExploredTest = IsExploredCore;
			isVisibleTest = IsVisibleCore;
		}

		void Invalidate(IEnumerable<CPos> changed)
		{
			if (CellsChanged != null)
				CellsChanged(changed);

			var oldHash = Hash;
			Hash = Sync.HashPlayer(self.Owner) + self.World.WorldTick * 3;

			// Invalidate may be called multiple times in one world tick, which is decoupled from rendering.
			if (oldHash == Hash)
				Hash += 1;
		}

		public static IEnumerable<CPos> CellsInRange(Map map, WPos pos, WDist range)
		{
			var r = (range.Range + 1023) / 1024;
			var limit = range.RangeSquared;
			var cell = map.CellContaining(pos);

			foreach (var c in map.FindTilesInCircle(cell, r, true))
				if ((map.CenterOfCell(c) - pos).HorizontalLengthSquared <= limit)
					yield return c;
		}

		public static IEnumerable<CPos> CellsInRange(Map map, CPos cell, WDist range)
		{
			return CellsInRange(map, map.CenterOfCell(cell), range);
		}

		public void AddVisibility(Actor a, CPos[] visible)
		{
			if (!a.Owner.IsAlliedWith(self.Owner))
				return;

			foreach (var c in visible)
			{
				var uv = c.ToMPos(map);

				// Force cells outside the visible bounds invisible
				if (!map.Contains(uv))
					continue;

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
			CPos[] visible;
			if (!visibility.TryGetValue(a, out visible))
				return;

			foreach (var c in visible)
			{
				// Cells outside the visible bounds don't increment visibleCount
				if (map.Contains(c))
					visibleCount[c.ToMPos(map)]--;
			}

			visibility.Remove(a);
			Invalidate(visible);
		}

		public void AddShroudGeneration(Actor a, CPos[] shrouded)
		{
			if (a.Owner.IsAlliedWith(self.Owner))
				return;

			foreach (var c in shrouded)
				generatedShroudCount[c]++;

			if (generation.ContainsKey(a))
				throw new InvalidOperationException("Attempting to add duplicate shroud generation");

			generation[a] = shrouded;
			Invalidate(shrouded);
		}

		public void RemoveShroudGeneration(Actor a)
		{
			CPos[] shrouded;
			if (!generation.TryGetValue(a, out shrouded))
				return;

			foreach (var c in shrouded)
				generatedShroudCount[c]--;

			generation.Remove(a);
			Invalidate(shrouded);
		}

		public void UpdatePlayerStance(World w, Player player, Stance oldStance, Stance newStance)
		{
			if (oldStance == newStance)
				return;

			foreach (var a in w.Actors.Where(a => a.Owner == player))
			{
				CPos[] visible = null;
				CPos[] shrouded = null;
				foreach (var p in self.World.Players)
				{
					if (p.Shroud.visibility.TryGetValue(self, out visible))
					{
						p.Shroud.RemoveVisibility(self);
						p.Shroud.AddVisibility(self, visible);
					}

					if (p.Shroud.generation.TryGetValue(self, out shrouded))
					{
						p.Shroud.RemoveShroudGeneration(self);
						p.Shroud.AddShroudGeneration(self, shrouded);
					}
				}
			}
		}

		public void Explore(World world, IEnumerable<CPos> cells)
		{
			var changed = new HashSet<CPos>();
			foreach (var c in cells)
			{
				if (!explored[c])
				{
					explored[c] = true;
					changed.Add(c);
				}
			}

			Invalidate(changed);
		}

		public void Explore(Shroud s)
		{
			if (map.Bounds != s.map.Bounds)
				throw new ArgumentException("The map bounds of these shrouds do not match.", "s");

			var changed = new List<CPos>();
			foreach (var uv in map.CellsInsideBounds.MapCoords)
			{
				if (!explored[uv] && s.explored[uv])
				{
					explored[uv] = true;
					changed.Add(uv.ToCPos(map));
				}
			}

			Invalidate(changed);
		}

		public void ExploreAll(World world)
		{
			var changed = new List<CPos>();
			foreach (var uv in map.CellsInsideBounds.MapCoords)
			{
				if (!explored[uv])
				{
					explored[uv] = true;
					changed.Add(uv.ToCPos(map));
				}
			}

			Invalidate(changed);
		}

		public void ResetExploration()
		{
			var changed = new List<CPos>();
			foreach (var uv in map.CellsInsideBounds.MapCoords)
			{
				var visible = visibleCount[uv] > 0;
				if (explored[uv] != visible)
				{
					explored[uv] = visible;
					changed.Add(uv.ToCPos(map));
				}
			}

			Invalidate(changed);
		}

		public bool IsExplored(WPos pos)
		{
			return IsExplored(map.CellContaining(pos));
		}

		public bool IsExplored(CPos cell)
		{
			return IsExplored(cell.ToMPos(map));
		}

		public bool IsExplored(MPos uv)
		{
			if (!map.Contains(uv))
				return false;

			if (!ShroudEnabled)
				return true;

			return IsExploredCore(uv);
		}

		bool ShroudEnabled { get { return !Disabled && self.World.LobbyInfo.GlobalSettings.Shroud; } }

		bool IsExploredCore(MPos uv)
		{
			return explored[uv] && (generatedShroudCount[uv] == 0 || visibleCount[uv] > 0);
		}

		/// <summary>
		/// Returns a fast exploration lookup that skips the usual validation.
		/// The return value should not be cached across ticks, and should not
		/// be called with cells outside the map bounds.
		/// </summary>
		public Func<MPos, bool> IsExploredTest
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
			return IsVisible(map.CellContaining(pos));
		}

		public bool IsVisible(CPos cell)
		{
			var uv = cell.ToMPos(map);
			return IsVisible(uv);
		}

		public bool IsVisible(MPos uv)
		{
			if (!map.Contains(uv))
				return false;

			if (!FogEnabled)
				return true;

			return IsVisibleCore(uv);
		}

		bool FogEnabled { get { return !Disabled && self.World.LobbyInfo.GlobalSettings.Fog; } }

		bool IsVisibleCore(MPos uv)
		{
			return visibleCount[uv] > 0;
		}

		/// <summary>
		/// Returns a fast visibility lookup that skips the usual validation.
		/// The return value should not be cached across ticks, and should not
		/// be called with cells outside the map bounds.
		/// </summary>
		public Func<MPos, bool> IsVisibleTest
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

		public bool Contains(MPos uv)
		{
			// Check that uv is inside the map area. There is nothing special
			// about explored here: any of the CellLayers would have been suitable.
			return explored.Contains(uv);
		}
	}
}
