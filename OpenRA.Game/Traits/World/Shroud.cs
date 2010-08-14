#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Traits
{
	public class ShroudInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Shroud(init.world); }
	}

	public class Shroud
	{
		Map map;

		public int[,] visibleCells;
		public bool[,] exploredCells;
		public Rectangle? exploredBounds;
		public event Action Dirty = () => { };

		public Shroud(World world)
		{
			map = world.Map;
			visibleCells = new int[map.MapSize.X, map.MapSize.Y];
			exploredCells = new bool[map.MapSize.X, map.MapSize.Y];

			world.ActorAdded += AddActor;
			world.ActorRemoved += RemoveActor;
		}

		// cache of positions that were added, so no matter what crazy trait code does, it
		// can't make us invalid.
		class ActorVisibility { public int range; public int2[] vis; }
		Dictionary<Actor, ActorVisibility> vis = new Dictionary<Actor, ActorVisibility>();

		static IEnumerable<int2> FindVisibleTiles(World world, int2 a, int r)
		{
			var min = a - new int2(r, r);
			var max = a + new int2(r, r);
			if (min.X < world.Map.XOffset - 1) min.X = world.Map.XOffset - 1;
			if (min.Y < world.Map.YOffset - 1) min.Y = world.Map.YOffset - 1;
			if (max.X > world.Map.XOffset + world.Map.Width) max.X = world.Map.XOffset + world.Map.Width;
			if (max.Y > world.Map.YOffset + world.Map.Height) max.Y = world.Map.YOffset + world.Map.Height;

			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (r * r >= (new int2(i, j) - a).LengthSquared)
						yield return new int2(i, j);
		}

		void AddActor(Actor a)
		{
			if (!a.HasTrait<RevealsShroud>())
				return;
						
			if (a.Owner == null || a.Owner.World.LocalPlayer == null 
			    || a.Owner.Stances[a.Owner.World.LocalPlayer] != Stance.Ally) return;

			if (vis.ContainsKey(a))
			{
				Game.Debug("Warning: Actor {0}:{1} at {2} bad vis".F(a.Info.Name, a.ActorID, a.Location));
				RemoveActor(a);
			}

			var v = new ActorVisibility
			{
				range = a.Trait<RevealsShroud>().RevealRange,
				vis = GetVisOrigins(a).ToArray()
			};

			if (v.range == 0) return;		// don't bother for things that can't see

			foreach (var p in v.vis)
			{
				foreach (var q in FindVisibleTiles(a.World, p, v.range))
				{
					++visibleCells[q.X, q.Y];
					exploredCells[q.X, q.Y] = true;
				}

				var box = new Rectangle(p.X - v.range, p.Y - v.range, 2 * v.range + 1, 2 * v.range + 1);
				exploredBounds = exploredBounds.HasValue ?
					Rectangle.Union(exploredBounds.Value, box) : box;
			}

			vis[a] = v;

			Dirty();
		}
		
		public void UpdatePlayerStance(World w, Player player, Stance oldStance, Stance newStance)
		{
			if (oldStance == newStance)
				return;
			
			// No longer our ally; remove unit vis
			if (oldStance == Stance.Ally)
			{
				var toRemove = new List<Actor>(vis.Select(a => a.Key).Where(a => a.Owner == player));
				foreach (var a in toRemove)
					RemoveActor(a);
			}
			// Is now our ally; add unit vis
			if (newStance == Stance.Ally)
				foreach (var a in w.Queries.OwnedBy[player])
					AddActor(a);
		}

		public static IEnumerable<int2> GetVisOrigins(Actor a)
		{
			if (a.Info.Traits.Contains<BuildingInfo>())
			{
				var bi = a.Info.Traits.Get<BuildingInfo>();
				return Footprint.Tiles(a.Info.Name, bi, a.Location);
			}
			else
			{
				var mobile = a.TraitOrDefault<Mobile>();
				if (mobile != null)
					return new[] { mobile.fromCell, mobile.toCell };
				else
					return new[] { (1f / Game.CellSize * a.CenterLocation).ToInt2() };
			}
		}

		void RemoveActor(Actor a)
		{
			ActorVisibility v;
			if (!vis.TryGetValue(a, out v)) return;

			foreach (var p in v.vis)
				foreach (var q in FindVisibleTiles(a.World, p, v.range))
					--visibleCells[q.X, q.Y];

			vis.Remove(a);

			Dirty();
		}

		public void UpdateActor(Actor a)
		{
			if (a.Owner == null || a.Owner.World.LocalPlayer == null 
			    || a.Owner.Stances[a.Owner.World.LocalPlayer] != Stance.Ally) return;

			RemoveActor(a); AddActor(a);
		}

		public void Explore(World world, int2 center, int range)
		{
			foreach (var q in FindVisibleTiles(world, center, range))
				exploredCells[q.X, q.Y] = true;

			var box = new Rectangle(center.X - range, center.Y - range, 2 * range + 1, 2 * range + 1);
			exploredBounds = exploredBounds.HasValue ?
				Rectangle.Union(exploredBounds.Value, box) : box;

			Dirty();
		}

		public void ResetExploration()		// for `hide map` crate
		{
			for (var j = 0; j <= exploredCells.GetUpperBound(1); j++)
				for (var i = 0; i <= exploredCells.GetUpperBound(0); i++)
					exploredCells[i, j] = visibleCells[i, j] > 0;

			Dirty();
		}
	}
}
