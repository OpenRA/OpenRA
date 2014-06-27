#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OpenRA.Traits
{
	public class ShroudInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Shroud(init.self); }
	}

	public class Shroud
	{
		[Sync] public bool Disabled = false;

		Actor self;
		Map map;

		CellLayer<int> visibleCount;
		CellLayer<int> generatedShroudCount;
		CellLayer<bool> explored;

		readonly Lazy<IFogVisibilityModifier[]> fogVisibilities;

		// Cache of visibility that was added, so no matter what crazy trait code does, it
		// can't make us invalid.
		Dictionary<Actor, CPos[]> visibility = new Dictionary<Actor, CPos[]>();
		Dictionary<Actor, CPos[]> generation = new Dictionary<Actor, CPos[]>();

		public int Hash { get; private set; }

		public Shroud(Actor self)
		{
			this.self = self;
			map = self.World.Map;

			visibleCount = new CellLayer<int>(map);
			generatedShroudCount = new CellLayer<int>(map);
			explored = new CellLayer<bool>(map);

			self.World.ActorAdded += AddVisibility;
			self.World.ActorRemoved += RemoveVisibility;

			self.World.ActorAdded += AddShroudGeneration;
			self.World.ActorRemoved += RemoveShroudGeneration;

			fogVisibilities = Exts.Lazy(() => self.TraitsImplementing<IFogVisibilityModifier>().ToArray());
		}

		void Invalidate()
		{
			Hash = Sync.hash_player(self.Owner) + self.World.WorldTick * 3;
		}

		static IEnumerable<CPos> FindVisibleTiles(World world, CPos position, WRange radius)
		{
			var r = (radius.Range + 1023) / 1024;
			var limit = radius.Range * radius.Range;
			var pos = position.CenterPosition;

			foreach (var cell in world.Map.FindTilesInCircle(position, r))
				if ((cell.CenterPosition - pos).HorizontalLengthSquared <= limit)
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

			return new[] { a.CenterPosition.ToCPos() };
		}

		public void Explore(World world, CPos center, WRange range)
		{
			foreach (var q in FindVisibleTiles(world, center, range))
				explored[q] = true;

			Invalidate();
		}

		public void Explore(Shroud s)
		{
			foreach (var cell in map.Cells)
				if (s.explored[cell])
					explored[cell] = true;

			Invalidate();
		}

		public void ExploreAll(World world)
		{
			explored.Clear(true);

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

			if (Disabled || !self.World.LobbyInfo.GlobalSettings.Shroud)
				return true;

			return explored[cell] && (generatedShroudCount[cell] == 0 || visibleCount[cell] > 0);
		}

		public bool IsExplored(Actor a)
		{
			return GetVisOrigins(a).Any(o => IsExplored(o));
		}

		public bool IsVisible(CPos cell)
		{
			if (!map.Contains(cell))
				return false;

			if (Disabled || !self.World.LobbyInfo.GlobalSettings.Fog)
				return true;

			return visibleCount[cell] > 0;
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
