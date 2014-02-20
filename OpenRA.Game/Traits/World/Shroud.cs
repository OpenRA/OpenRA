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

		int[,] visibleCount;
		int[,] generatedShroudCount;
		bool[,] explored;

		// Cache of visibility that was added, so no matter what crazy trait code does, it
		// can't make us invalid.
		Dictionary<Actor, CPos[]> visibility = new Dictionary<Actor, CPos[]>();
		Dictionary<Actor, CPos[]> generation = new Dictionary<Actor, CPos[]>();

		public Rectangle ExploredBounds { get; private set; }

		public int Hash { get; private set; }

		public Shroud(Actor self)
		{
			this.self = self;
			map = self.World.Map;

			visibleCount = new int[map.MapSize.X, map.MapSize.Y];
			generatedShroudCount = new int[map.MapSize.X, map.MapSize.Y];
			explored = new bool[map.MapSize.X, map.MapSize.Y];

			self.World.ActorAdded += AddVisibility;
			self.World.ActorRemoved += RemoveVisibility;

			self.World.ActorAdded += AddShroudGeneration;
			self.World.ActorRemoved += RemoveShroudGeneration;

			if (!self.World.LobbyInfo.GlobalSettings.Shroud)
				ExploredBounds = map.Bounds;
		}

		void Invalidate()
		{
			Hash = Sync.hash_player(self.Owner) + self.World.FrameNumber * 3;
		}

		static IEnumerable<CPos> FindVisibleTiles(World world, CPos position, WRange radius)
		{
			var r = (radius.Range + 1023) / 1024;
			var min = (position - new CVec(r, r)).Clamp(world.Map.Bounds);
			var max = (position + new CVec(r, r)).Clamp(world.Map.Bounds);

			var circleArea = radius.Range * radius.Range;
			var pos = position.CenterPosition;
			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (circleArea >= (new CPos(i, j).CenterPosition - pos).LengthSquared)
						yield return new CPos(i, j);
		}

		void AddVisibility(Actor a)
		{
			var rs = a.TraitOrDefault<RevealsShroud>();
			if (rs == null || !a.Owner.IsAlliedWith(self.Owner) || rs.Range == WRange.Zero)
				return;

			var origins = GetVisOrigins(a);
			var visible = origins.SelectMany(o => FindVisibleTiles(a.World, o, rs.Range))
				.Distinct().ToArray();

			// Update bounding rect
			var r = (rs.Range.Range + 1023) / 1024;

			foreach (var o in origins)
			{
				var box = new Rectangle(o.X - r, o.Y - r, 2 * r + 1, 2 * r + 1);
				ExploredBounds = Rectangle.Union(ExploredBounds, box);
			}

			// Update visibility
			foreach (var c in visible)
			{
				visibleCount[c.X, c.Y]++;
				explored[c.X, c.Y] = true;
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
				visibleCount[c.X, c.Y]--;

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
				generatedShroudCount[c.X, c.Y]++;

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
				generatedShroudCount[c.X, c.Y]--;

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
				explored[q.X, q.Y] = true;

			var r = (range.Range + 1023) / 1024;
			var box = new Rectangle(center.X - r, center.Y - r, 2 * r + 1, 2 * r + 1);
			ExploredBounds = Rectangle.Union(ExploredBounds, box);

			Invalidate();
		}

		public void Explore(Shroud s)
		{
			for (var i = map.Bounds.Left; i < map.Bounds.Right; i++)
				for (var j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
					if (s.explored[i,j] == true)
						explored[i, j] = true;

			ExploredBounds = Rectangle.Union(ExploredBounds, s.ExploredBounds);
		}

		public void ExploreAll(World world)
		{
			for (var i = map.Bounds.Left; i < map.Bounds.Right; i++)
				for (var j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
					explored[i, j] = true;

			ExploredBounds = world.Map.Bounds;

			Invalidate();
		}

		public void ResetExploration()
		{
			for (var i = map.Bounds.Left; i < map.Bounds.Right; i++)
				for (var j = map.Bounds.Top; j < map.Bounds.Bottom; j++)
					explored[i, j] = visibleCount[i, j] > 0;

			Invalidate();
		}

		public bool IsExplored(CPos xy) { return IsExplored(xy.X, xy.Y); }
		public bool IsExplored(int x, int y)
		{
			if (!map.IsInMap(x, y))
				return false;

			if (Disabled || !self.World.LobbyInfo.GlobalSettings.Shroud)
				return true;

			return explored[x, y] && (generatedShroudCount[x, y] == 0 || visibleCount[x, y] > 0);
		}

		public bool IsExplored(Actor a)
		{
			return GetVisOrigins(a).Any(o => IsExplored(o));
		}

		public bool IsVisible(CPos xy) { return IsVisible(xy.X, xy.Y); }
		public bool IsVisible(int x, int y)
		{
			if (!map.IsInMap(x, y))
				return false;

			if (Disabled || !self.World.LobbyInfo.GlobalSettings.Fog)
				return true;

			return visibleCount[x, y] > 0;
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
			if (a.TraitsImplementing<IVisibilityModifier>().Any(t => !t.IsVisible(a, self.Owner)))
				return false;

			return GetVisOrigins(a).Any(o => IsVisible(o));
		}
	}
}
