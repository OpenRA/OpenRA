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
using OpenRA.Graphics;

namespace OpenRA.Traits
{
	public class ShroudInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Shroud(init.self); }
	}

	public class Shroud
	{
		Map map;
		Actor self;

		int[,] visibleCells;
		bool[,] exploredCells;
		bool[,] foggedCells;

		public Rectangle ExploredBounds { get; private set; }

		public int Hash { get; private set; }

		public Shroud(Actor self)
		{
			this.self = self;
			map = self.World.Map;

			visibleCells = new int[map.MapSize.X, map.MapSize.Y];
			exploredCells = new bool[map.MapSize.X, map.MapSize.Y];
			foggedCells = new bool[map.MapSize.X, map.MapSize.Y];
			self.World.ActorAdded += AddActor;
			self.World.ActorRemoved += RemoveActor;
		}

		void Invalidate()
		{
			Hash = Sync.hash_player(self.Owner) + self.World.FrameNumber * 3;
		}

		// cache of positions that were added, so no matter what crazy trait code does, it
		// can't make us invalid.
		public class ActorVisibility
		{
			[Sync] public int range;
			[Sync] public CPos[] vis;
		}

		public Dictionary<Actor, ActorVisibility> vis = new Dictionary<Actor, ActorVisibility>();

		static IEnumerable<CPos> FindVisibleTiles(World world, CPos a, int r)
		{
			var min = a - new CVec(r, r);
			var max = a + new CVec(r, r);
			if (min.X < world.Map.Bounds.Left - 1)
				min = new CPos(world.Map.Bounds.Left - 1, min.Y);

			if (min.Y < world.Map.Bounds.Top - 1)
				min = new CPos(min.X, world.Map.Bounds.Top - 1);

			if (max.X > world.Map.Bounds.Right)
				max = new CPos(world.Map.Bounds.Right, max.Y);

			if (max.Y > world.Map.Bounds.Bottom)
				max = new CPos(max.X, world.Map.Bounds.Bottom);

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (r * r >= (new CPos(i, j) - a).LengthSquared)
						yield return new CPos(i, j);
		}

		public void AddActor(Actor a)
		{
			if (!a.HasTrait<RevealsShroud>() || !a.Owner.IsAlliedWith(self.Owner))
				return;

			ActorVisibility v = a.Sight;
			if (v.range == 0)
				return;

			foreach (var p in v.vis)
			{
				foreach (var q in FindVisibleTiles(a.World, p, v.range))
				{
					++visibleCells[q.X, q.Y];
					exploredCells[q.X, q.Y] = true;
					foggedCells[q.X, q.Y] = true;
				}

				var box = new Rectangle(p.X - v.range, p.Y - v.range, 2 * v.range + 1, 2 * v.range + 1);
				ExploredBounds = Rectangle.Union(ExploredBounds, box);
			}

			Invalidate();
		}

		public void HideActor(Actor a, int range)
		{
			if (a.Owner.IsAlliedWith(self.Owner))
				return;

			var v = new ActorVisibility
			{
				vis = GetVisOrigins(a).ToArray()
			};

			foreach (var p in v.vis)
				foreach (var q in FindVisibleTiles(a.World, p, range))
					foggedCells[q.X, q.Y] = visibleCells[q.X, q.Y] > 0;

			Invalidate();
		}

		public void UnhideActor(Actor a, ActorVisibility v, int range)
		{
			if (a.Owner.IsAlliedWith(self.Owner) || v == null)
				return;

	 		foreach (var p in v.vis)
				foreach (var q in FindVisibleTiles(a.World, p, range))
					foggedCells[q.X, q.Y] = exploredCells[q.X, q.Y];

			Invalidate();
		}

		public void MergeShroud(Shroud s)
		{
			for (int i = map.Bounds.Left; i < map.Bounds.Right; i++)
			{
				for (int j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
				{
					if (s.exploredCells[i,j] == true)
						exploredCells[i, j] = true;
					if (s.foggedCells[i,j] == true)
						foggedCells[i, j] = true;
				}
				ExploredBounds = Rectangle.Union(ExploredBounds, s.ExploredBounds);
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
					if (foggedCells[i, j])
						seen++;

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
			ActorVisibility v = a.Sight;
			if (!a.Owner.IsAlliedWith(self.Owner))
			{
				if (a.HasTrait<CreatesShroud>())
					foreach (var p in v.vis)
						foreach (var q in FindVisibleTiles(a.World, p, v.range))
							foggedCells[q.X, q.Y] = exploredCells[q.X, q.Y];
				return;
			}

			if (!a.HasTrait<RevealsShroud>())
				return;

			foreach (var p in v.vis)
				foreach (var q in FindVisibleTiles(a.World, p, v.range))
					--visibleCells[q.X, q.Y];

			Invalidate();
		}

		public void UpdateActor(Actor a)
		{
			if (!a.Owner.IsAlliedWith(self.Owner))
				return;

			RemoveActor(a);
			AddActor(a);
		}

		public void Explore(World world, CPos center, int range)
		{
			foreach (var q in FindVisibleTiles(world, center, range)) {
				exploredCells[q.X, q.Y] = true;
				foggedCells[q.X, q.Y] = true;
			}

			var box = new Rectangle(center.X - range, center.Y - range, 2 * range + 1, 2 * range + 1);
			ExploredBounds = Rectangle.Union(ExploredBounds, box);

			Invalidate();
		}

		public void ExploreAll(World world)
		{
			for (int i = map.Bounds.Left; i < map.Bounds.Right; i++) {
				for (int j = map.Bounds.Top; j < map.Bounds.Bottom; j++) {
					exploredCells[i, j] = true;
					foggedCells[i, j] = true;
				}
			}
			ExploredBounds = world.Map.Bounds;

			Invalidate();
		}

		public void ResetExploration()
		{
			for (var j = 0; j <= exploredCells.GetUpperBound(1); j++)
				for (var i = 0; i <= exploredCells.GetUpperBound(0); i++)
					exploredCells[i, j] = visibleCells[i, j] > 0;

			for (var j = 0; j <= foggedCells.GetUpperBound(1); j++)
				for (var i = 0; i <= foggedCells.GetUpperBound(0); i++)
					foggedCells[i, j] = visibleCells[i, j] > 0;

			Invalidate();
		}

		public bool IsExplored(CPos xy) { return IsExplored(xy.X, xy.Y); }
		public bool IsExplored(int x, int y)
		{
			if (!map.IsInMap(x, y))
				return false;

			return foggedCells[x,y];
		}

		public bool IsExplored(Actor a)
		{
			return GetVisOrigins(a).Any(o => IsExplored(o));
		}

		public bool IsVisible(CPos xy) { return IsVisible(xy.X, xy.Y); }
		public bool IsVisible(int x, int y)
		{
			// Visibility is allowed to extend beyond the map cordon so that
			// the fog tiles are not visible at the edge of the world
			if (x < 0 || x >= map.MapSize.X || y < 0 || y >= map.MapSize.Y)
				return false;

			return visibleCells[x,y] != 0;
		}

		// Actors are hidden under shroud, but not under fog by default
		public bool IsVisible(Actor a)
		{
			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, self.Owner)))
				return false;

			return a.Owner.IsAlliedWith(self.Owner) || IsExplored(a);
		}

		public bool IsTargetable(Actor a) {
			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, self.Owner)))
				return false;

			return GetVisOrigins(a).Any(o => IsVisible(o));
		}
	}
}
