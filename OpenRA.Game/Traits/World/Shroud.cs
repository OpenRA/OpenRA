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
		public object Create(ActorInitializer init) { return new Shroud(init.self); }
	}

	public class Shroud
	{
		[Sync] public bool Disabled;

		readonly Actor self;
		readonly Map map;

		readonly CellLayer<short> visibleCount;
		readonly CellLayer<short> generatedShroudCount;
		readonly CellLayer<bool> explored;

		readonly Lazy<IFogVisibilityModifier[]> fogVisibilities;

		// Cache of visibility that was added, so no matter what crazy trait code does, it
		// can't make us invalid.
		readonly Dictionary<Actor, CPos[]> visibility = new Dictionary<Actor, CPos[]>();
		readonly Dictionary<Actor, CPos[]> generation = new Dictionary<Actor, CPos[]>();

		public int Hash { get; private set; }

		static readonly Func<CPos, bool> TruthPredicate = cell => true;
		readonly Func<CPos, bool> shroudEdgeTest;
		readonly Func<CPos, bool> fastExploredTest;
		readonly Func<CPos, bool> slowExploredTest;
		readonly Func<CPos, bool> fastVisibleTest;
		readonly Func<CPos, bool> slowVisibleTest;

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

			shroudEdgeTest = cell => map.Contains(cell);
			fastExploredTest = IsExploredCore;
			slowExploredTest = IsExplored;
			fastVisibleTest = IsVisibleCore;
			slowVisibleTest = IsVisible;
		}

		void Invalidate()
		{
			Hash = Sync.hash_player(self.Owner) + self.World.WorldTick * 3;
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

			foreach (var cell in map.Cells)
				if (s.explored[cell])
					explored[cell] = true;

			Invalidate();
		}

		public void ExploreAll(World world)
		{
			foreach (var cell in map.Cells)
				explored[cell] = true;

			Invalidate();
		}

		public void ResetExploration()
		{
			foreach (var cell in map.Cells)
				explored[cell] = visibleCount[cell] > 0;

			Invalidate();
		}

		public bool IsExplored(CPos cell)
		{
			if (!map.Contains(cell))
				return false;

			if (!ShroudEnabled)
				return true;

			return IsExploredCore(cell);
		}

		bool ShroudEnabled { get { return !Disabled && self.World.LobbyInfo.GlobalSettings.Shroud; } }

		bool IsExploredCore(CPos cell)
		{
			var uv = Map.CellToMap(map.TileShape, cell);
			return explored[uv.X, uv.Y] && (generatedShroudCount[uv.X, uv.Y] == 0 || visibleCount[uv.X, uv.Y] > 0);
		}

		public Func<CPos, bool> IsExploredTest(CellRegion region)
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
			if (!map.Contains(cell))
				return false;

			if (!FogEnabled)
				return true;

			return IsVisibleCore(cell);
		}

		bool FogEnabled { get { return !Disabled && self.World.LobbyInfo.GlobalSettings.Fog; } }

		bool IsVisibleCore(CPos cell)
		{
			return visibleCount[cell] > 0;
		}

		public Func<CPos, bool> IsVisibleTest(CellRegion region)
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
