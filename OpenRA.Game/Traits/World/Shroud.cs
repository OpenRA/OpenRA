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
		public object Create(ActorInitializer init) { return new Shroud(init.world); }
	}

	public class Shroud
	{
		Map map;
		World world;

		public Player Owner;
		public int[,] visibleCells;
		public bool[,] exploredCells;
		public bool[,] foggedCells;
		public Rectangle? exploredBounds;
		bool disabled = false;
		public bool dirty = true;
		public bool Disabled
		{
			get { return disabled; }
			set { disabled = value; Dirty(); }
		}

		public bool Observing
		{
			get { return world.IsShellmap || (world.LocalPlayer == null && Owner == null);; }
		}

		public Rectangle? Bounds
		{
			get { return Disabled ? null : exploredBounds; }
		}

		public event Action Dirty = () => { };

		public void Jank()
		{
			Dirty();
		}


		public Shroud(World world)
		{
			this.world = world;
			map = world.Map;
			visibleCells = new int[map.MapSize.X, map.MapSize.Y];
			exploredCells = new bool[map.MapSize.X, map.MapSize.Y];
			foggedCells = new bool[map.MapSize.X, map.MapSize.Y];
			world.ActorAdded += AddActor;
			world.ActorRemoved += RemoveActor;
			Dirty += () => dirty = true;
		}

		// cache of positions that were added, so no matter what crazy trait code does, it
		// can't make us invalid.
		public class ActorVisibility { public int range; public CPos[] vis; }
		public Dictionary<Actor, ActorVisibility> vis = new Dictionary<Actor, ActorVisibility>();

		static IEnumerable<CPos> FindVisibleTiles(World world, CPos a, int r)
		{
			var min = a - new CVec(r, r);
			var max = a + new CVec(r, r);
			if (min.X < world.Map.Bounds.Left - 1) min = new CPos(world.Map.Bounds.Left - 1, min.Y);
			if (min.Y < world.Map.Bounds.Top - 1) min = new CPos(min.X, world.Map.Bounds.Top - 1);
			if (max.X > world.Map.Bounds.Right) max = new CPos(world.Map.Bounds.Right, max.Y);
			if (max.Y > world.Map.Bounds.Bottom) max = new CPos(max.X, world.Map.Bounds.Bottom);

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (r * r >= (new CPos(i, j) - a).LengthSquared)
						yield return new CPos(i, j);
		}

		public void AddActor(Actor a)
		{
			if (!a.HasTrait<RevealsShroud>()) return;
			if (a.Owner == null || Owner == null) return;
			if(a.Owner.Stances[Owner] != Stance.Ally) return;

			ActorVisibility v = a.Sight;

			if (v.range == 0) return;		// don't bother for things that can't see

			foreach (var p in v.vis)
			{
				foreach (var q in FindVisibleTiles(a.World, p, v.range))
				{
					++visibleCells[q.X, q.Y];
					exploredCells[q.X, q.Y] = true;
					foggedCells[q.X, q.Y] = true;
				}

				var box = new Rectangle(p.X - v.range, p.Y - v.range, 2 * v.range + 1, 2 * v.range + 1);
				exploredBounds = (exploredBounds.HasValue) ? Rectangle.Union(exploredBounds.Value, box) : box;
			}

			if (!Disabled)
				Dirty();
		}

		public void HideActor(Actor a, int range)
		{
			if (a.Owner.World.LocalPlayer == null
				|| a.Owner.Stances[a.Owner.World.LocalPlayer] == Stance.Ally) return;

			var v = new ActorVisibility
			{
				vis = GetVisOrigins(a).ToArray()
			};

			foreach (var p in v.vis)
				foreach (var q in FindVisibleTiles(a.World, p, range))
					foggedCells[q.X, q.Y] = visibleCells[q.X, q.Y] > 0;

			if (!Disabled)
				Dirty();
		}

		public void UnhideActor(Actor a, ActorVisibility v, int range) {
	 		if (a.Owner.World.LocalPlayer == null
				|| a.Owner.Stances[a.Owner.World.LocalPlayer] == Stance.Ally) return;

			if (v == null)
				return;

	 		foreach (var p in v.vis)
				foreach (var q in FindVisibleTiles(a.World, p, range))
					foggedCells[q.X, q.Y] = exploredCells[q.X, q.Y];

	 		if (!Disabled)
				Dirty();
		}

		public void MergeShroud(Shroud s) {
			for (int i = map.Bounds.Left; i < map.Bounds.Right; i++) {
				for (int j = map.Bounds.Top; j < map.Bounds.Bottom; j++) {
					if (s.exploredCells[i,j] == true)
						exploredCells[i, j] = true;
					if (s.foggedCells[i,j] == true)
						foggedCells[i, j] = true;
				}
				exploredBounds = Rectangle.Union(exploredBounds.Value, s.exploredBounds.Value);
			}
		}

		public void UpdatePlayerStance(World w, Player player, Stance oldStance, Stance newStance)
		{
			if (oldStance == newStance)
				return;

			// No longer our ally; remove unit vis
			if (oldStance == Stance.Ally)
			{
				var toRemove =  w.Actors.Where(a => a.Owner == player).ToList();
				foreach (var a in toRemove)
					RemoveActor(a);
			}
			// Is now our ally; add unit vis
			if (newStance == Stance.Ally)
				foreach (var a in w.Actors.Where( a => a.Owner == player ))
					AddActor(a);
		}

		public int Explored()
		{
			int seen = 0;
			for (int i = map.Bounds.Left; i < map.Bounds.Right; i++)
				for (int j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
					if(foggedCells[i, j]) seen++;

			return seen;
		}

		public static IEnumerable<CPos> GetVisOrigins(Actor a)
		{
			var ios = a.OccupiesSpace;
			if (ios != null)
			{
				var cells = ios.OccupiedCells();
				if (cells.Any()) return cells.Select(c => c.First);
			}

			return new[] { a.CenterLocation.ToCPos() };
		}

		public void RemoveActor(Actor a)
		{
			if (!a.HasTrait<RevealsShroud>())return;
			if (a.Owner == null || Owner == null) return;

			ActorVisibility v = a.Sight;

			if(a.Owner.Stances[Owner] != Stance.Ally) {
				if (a.HasTrait<CreatesShroud>()) {
					foreach (var p in v.vis)
						foreach (var q in FindVisibleTiles(a.World, p, v.range))
							foggedCells[q.X, q.Y] = exploredCells[q.X, q.Y];
				}
				return;
			}

			foreach (var p in v.vis)
				foreach (var q in FindVisibleTiles(a.World, p, v.range))
					--visibleCells[q.X, q.Y];

			if (!Disabled)
				Dirty();
		}

		public void UpdateActor(Actor a)
		{
			if (a.Owner.World.LocalPlayer == null
				|| a.Owner.Stances[a.Owner.World.LocalPlayer] != Stance.Ally) return;

			RemoveActor(a); AddActor(a);
		}

		public void Explore(World world, CPos center, int range)
		{
			foreach (var q in FindVisibleTiles(world, center, range)) {
				exploredCells[q.X, q.Y] = true;
				foggedCells[q.X, q.Y] = true;
			}

			var box = new Rectangle(center.X - range, center.Y - range, 2 * range + 1, 2 * range + 1);
			exploredBounds = (exploredBounds.HasValue) ? Rectangle.Union(exploredBounds.Value, box) : box;

			if (!Disabled)
				Dirty();
		}

		public void ExploreAll(World world)
		{
			for (int i = map.Bounds.Left; i < map.Bounds.Right; i++) {
				for (int j = map.Bounds.Top; j < map.Bounds.Bottom; j++) {
					exploredCells[i, j] = true;
					foggedCells[i, j] = true;
				}
			}
			exploredBounds = world.Map.Bounds;

			if (!Disabled)
				Dirty();
		}

		public void ResetExploration()		// for `hide map` crate
		{
			for (var j = 0; j <= exploredCells.GetUpperBound(1); j++)
				for (var i = 0; i <= exploredCells.GetUpperBound(0); i++)
					exploredCells[i, j] = visibleCells[i, j] > 0;

			for (var j = 0; j <= foggedCells.GetUpperBound(1); j++)
				for (var i = 0; i <= foggedCells.GetUpperBound(0); i++)
					foggedCells[i, j] = visibleCells[i, j] > 0;

			if (!Disabled)
				Dirty();
		}

		public bool IsExplored(CPos xy) { return IsExplored(xy.X, xy.Y); }
		public bool IsExplored(int x, int y)
		{
			if (!map.IsInMap(x, y))
				return false;

			if (Disabled || Observing)
				return true;

			return foggedCells[x,y];
		}

		public bool IsVisible(CPos xy) { return IsVisible(xy.X, xy.Y); }
		public bool IsVisible(int x, int y)
		{
			if (Disabled || Observing)
				return true;

			// Visibility is allowed to extend beyond the map cordon so that
			// the fog tiles are not visible at the edge of the world
			if (x < 0 || x >= map.MapSize.X || y < 0 || y >= map.MapSize.Y)
				return false;

			return visibleCells[x,y] != 0;
		}

		// Actors are hidden under shroud, but not under fog by default
		public bool IsVisible(Actor a)
		{
			// I need to pass in the current shroud, otherwise we're just checking that true==true
			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(this, a)))
				return false;

			if(Owner == null) return true;

			return Disabled || Observing || a.Owner.Stances[Owner] == Stance.Ally || GetVisOrigins(a).Any(o => IsExplored(o));
		}

		public bool IsTargetable(Actor a) {
			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(this, a)))
				return false;

			return GetVisOrigins(a).Any(o => IsVisible(o));
		}
	}
}
