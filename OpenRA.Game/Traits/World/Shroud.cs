#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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

		readonly Actor self;
		readonly Map map;

		readonly CellLayer<short> visibleCount;
		readonly CellLayer<short> generatedShroudCount;
		readonly CellLayer<bool> explored;

		public event Action<CPos> CellEntryChanged
		{
			add
			{
				visibleCount.CellEntryChanged += value;
				generatedShroudCount.CellEntryChanged += value;
				explored.CellEntryChanged += value;
			}

			remove
			{
				visibleCount.CellEntryChanged -= value;
				generatedShroudCount.CellEntryChanged -= value;
				explored.CellEntryChanged -= value;
			}
		}

		readonly Lazy<IFogVisibilityModifier[]> fogVisibilities;

		// Cache of visibility that was added, so no matter what crazy trait code does, it
		// can't make us invalid.
		readonly Dictionary<Actor, CPos[]> visibility = new Dictionary<Actor, CPos[]>();
		readonly Dictionary<Actor, CPos[]> generation = new Dictionary<Actor, CPos[]>();

		public int Hash { get; private set; }

		static readonly Func<MPos, bool> TruthPredicate = _ => true;
		readonly Func<MPos, bool> shroudEdgeTest;
		readonly Func<MPos, bool> fastExploredTest;
		readonly Func<MPos, bool> slowExploredTest;
		readonly Func<MPos, bool> fastVisibleTest;
		readonly Func<MPos, bool> slowVisibleTest;

		public Shroud(Actor self)
		{
			this.self = self;
			map = self.World.Map;

			visibleCount = new CellLayer<short>(map);
			generatedShroudCount = new CellLayer<short>(map);
			explored = new CellLayer<bool>(map);

			self.World.ActorAdded += AddVisibility;
			self.World.ActorRemoved += RemoveVisibility;

			self.World.ActorAdded += AddShroudGeneration;
			self.World.ActorRemoved += RemoveShroudGeneration;

			fogVisibilities = Exts.Lazy(() => self.TraitsImplementing<IFogVisibilityModifier>().ToArray());

			shroudEdgeTest = map.Contains;
			fastExploredTest = IsExploredCore;
			slowExploredTest = IsExplored;
			fastVisibleTest = IsVisibleCore;
			slowVisibleTest = IsVisible;
		}

		void Invalidate()
		{
			var oldHash = Hash;
			Hash = Sync.HashPlayer(self.Owner) + self.World.WorldTick * 3;

			// Invalidate may be called multiple times in one world tick, which is decoupled from rendering.
			if (oldHash == Hash)
				Hash += 1;
		}

		static IEnumerable<CPos> FindVisibleTiles(World world, CPos position, WRange radius)
		{
			var map = world.Map;
			var r = (radius.Range + 1023) / 1024;
			var limit = radius.Range * radius.Range;
			var pos = map.CenterOfCell(position);

			foreach (var cell in map.FindTilesInCircle(position, r))
				if ((map.CenterOfCell(cell) - pos).HorizontalLengthSquared <= limit)
					yield return cell;
		}

		void AddVisibility(Actor a)
		{
			var rs = a.TraitOrDefault<RevealsShroud>();
			if (rs == null || !a.Owner.IsAlliedWith(self.Owner) || rs.Range == WRange.Zero)
				return;

			var origins = GetVisOrigins(a);
			var visible = origins.SelectMany(o => FindVisibleTiles(a.World, o, rs.Range))
				.Distinct().ToArray();

			// Update visibility
			foreach (var c in visible)
			{
				visibleCount[c]++;
				explored[c] = true;
			}

			if (visibility.ContainsKey(a))
				throw new InvalidOperationException("Attempting to add duplicate actor visibility");

			visibility[a] = visible;
			Invalidate();
		}

		void RemoveVisibility(Actor a)
		{
			CPos[] visible;
			if (!visibility.TryGetValue(a, out visible))
				return;

			foreach (var c in visible)
				visibleCount[c]--;

			visibility.Remove(a);
			Invalidate();
		}

		public void UpdateVisibility(Actor a)
		{
			// Actors outside the world don't have any vis
			if (!a.IsInWorld)
				return;

			RemoveVisibility(a);
			AddVisibility(a);
		}

		void AddShroudGeneration(Actor a)
		{
			var cs = a.TraitOrDefault<CreatesShroud>();
			if (cs == null || a.Owner.IsAlliedWith(self.Owner) || cs.Range == WRange.Zero)
				return;

			var shrouded = GetVisOrigins(a).SelectMany(o => FindVisibleTiles(a.World, o, cs.Range))
				.Distinct().ToArray();
			foreach (var c in shrouded)
				generatedShroudCount[c]++;

			if (generation.ContainsKey(a))
				throw new InvalidOperationException("Attempting to add duplicate shroud generation");

			generation[a] = shrouded;
			Invalidate();
		}

		void RemoveShroudGeneration(Actor a)
		{
			CPos[] shrouded;
			if (!generation.TryGetValue(a, out shrouded))
				return;

			foreach (var c in shrouded)
				generatedShroudCount[c]--;

			generation.Remove(a);
			Invalidate();
		}

		public void UpdateShroudGeneration(Actor a)
		{
			RemoveShroudGeneration(a);
			AddShroudGeneration(a);
		}

		public void UpdatePlayerStance(World w, Player player, Stance oldStance, Stance newStance)
		{
			if (oldStance == newStance)
				return;

			foreach (var a in w.Actors.Where(a => a.Owner == player))
			{
				UpdateVisibility(a);
				UpdateShroudGeneration(a);
			}
		}

		public static IEnumerable<CPos> GetVisOrigins(Actor a)
		{
			var ios = a.OccupiesSpace;
			if (ios != null)
			{
				var cells = ios.OccupiedCells();
				if (cells.Any())
					return cells.Select(c => c.First);
			}

			return new[] { a.World.Map.CellContaining(a.CenterPosition) };
		}

		public void Explore(World world, CPos center, WRange range)
		{
			foreach (var c in FindVisibleTiles(world, center, range))
				explored[c] = true;

			Invalidate();
		}

		public void Explore(Shroud s)
		{
			if (map.Bounds != s.map.Bounds)
				throw new ArgumentException("The map bounds of these shrouds do not match.", "s");

			foreach (var uv in map.Cells.MapCoords)
				if (s.explored[uv])
					explored[uv] = true;

			Invalidate();
		}

		public void ExploreAll(World world)
		{
			foreach (var uv in map.Cells.MapCoords)
				explored[uv] = true;

			Invalidate();
		}

		public void ResetExploration()
		{
			foreach (var uv in map.Cells.MapCoords)
				explored[uv] = visibleCount[uv] > 0;

			Invalidate();
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

		public Func<MPos, bool> IsExploredTest(CellRegion region)
		{
			// If the region to test extends outside the map we must use the slow test that checks the map boundary every time.
			if (!map.Cells.Contains(region))
				return slowExploredTest;

			// If shroud isn't enabled, then we can see everything inside the map.
			if (!ShroudEnabled)
				return shroudEdgeTest;

			// If shroud is enabled, we can use the fast test that just does the core check.
			return fastExploredTest;
		}

		public bool IsExplored(Actor a)
		{
			return GetVisOrigins(a).Any(o => IsExplored(o));
		}

		public bool IsVisible(CPos cell)
		{
			var uv = cell.ToMPos(map);
			return IsVisible(uv);
		}

		bool IsVisible(MPos uv)
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

		public Func<MPos, bool> IsVisibleTest(CellRegion region)
		{
			// If the region to test extends outside the map we must use the slow test that checks the map boundary every time.
			if (!map.Cells.Contains(region))
				return slowVisibleTest;

			// If fog isn't enabled, then we can see everything.
			if (!FogEnabled)
				return TruthPredicate;

			// If fog is enabled, we can use the fast test that just does the core check.
			return fastVisibleTest;
		}

		// Actors are hidden under shroud, but not under fog by default
		public bool IsVisible(Actor a)
		{
			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, self.Owner)))
				return false;

			return a.Owner.IsAlliedWith(self.Owner) || IsExplored(a);
		}

		public bool IsTargetable(Actor a)
		{
			if (HasFogVisibility())
				return true;

			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, self.Owner)))
				return false;

			return GetVisOrigins(a).Any(IsVisible);
		}

		public bool HasFogVisibility()
		{
			return fogVisibilities.Value.Any(f => f.HasFogVisibility(self.Owner));
		}
	}
}
