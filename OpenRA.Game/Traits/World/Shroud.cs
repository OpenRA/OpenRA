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
		[Sync] public bool Disabled;

		readonly Actor self;
		readonly Map map;

		readonly short[] visibleCount;
		readonly short[] generatedShroudCount;
		readonly bool[] explored;
		readonly int stride;

		readonly Lazy<IFogVisibilityModifier[]> fogVisibilities;

		// Cache of visibility that was added, so no matter what crazy trait code does, it
		// can't make us invalid.
		readonly Dictionary<Actor, CPos[]> visibility = new Dictionary<Actor, CPos[]>();
		readonly Dictionary<Actor, CPos[]> generation = new Dictionary<Actor, CPos[]>();

		public Rectangle ExploredBounds { get; private set; }

		public int Hash { get; private set; }

		public Shroud(Actor self)
		{
			this.self = self;
			map = self.World.Map;

			var mapArea = map.MapSize.X * map.MapSize.Y;
			stride = map.MapSize.X;
			visibleCount = new short[mapArea];
			generatedShroudCount = new short[mapArea];
			explored = new bool[mapArea];

			self.World.ActorAdded += AddVisibility;
			self.World.ActorRemoved += RemoveVisibility;

			self.World.ActorAdded += AddShroudGeneration;
			self.World.ActorRemoved += RemoveShroudGeneration;

			if (!self.World.LobbyInfo.GlobalSettings.Shroud)
				ExploredBounds = map.Bounds;

			fogVisibilities = Exts.Lazy(() => self.TraitsImplementing<IFogVisibilityModifier>().ToArray());
		}

		void Invalidate()
		{
			Hash = Sync.hash_player(self.Owner) + self.World.WorldTick * 3;
		}

		static IEnumerable<CPos> FindVisibleTiles(World world, CPos position, WRange radius)
		{
			var r = (radius.Range + 1023) / 1024;
			var min = (position - new CVec(r, r)).Clamp(world.Map.Bounds);
			var max = (position + new CVec(r, r)).Clamp(world.Map.Bounds);

			var circleArea = radius.Range * radius.Range;
			var pos = position.CenterPosition;
			for (var y = min.Y; y < max.Y; y++)
				for (var x = min.X; x < max.X; x++)
					if (circleArea >= (new CPos(x, y).CenterPosition - pos).LengthSquared)
						yield return new CPos(x, y);
		}

		int CPosToIndex(CPos c)
		{
			return PosToIndex(c.X, c.Y);
		}

		int PosToIndex(int x, int y)
		{
			return y * stride + x;
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
				var index = CPosToIndex(c);
				visibleCount[index]++;
				explored[index] = true;
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
				visibleCount[CPosToIndex(c)]--;

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
				generatedShroudCount[CPosToIndex(c)]++;

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
				generatedShroudCount[CPosToIndex(c)]--;

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
			foreach (var c in FindVisibleTiles(world, center, range))
				explored[CPosToIndex(c)] = true;

			var r = (range.Range + 1023) / 1024;
			var box = new Rectangle(center.X - r, center.Y - r, 2 * r + 1, 2 * r + 1);
			ExploredBounds = Rectangle.Union(ExploredBounds, box);

			Invalidate();
		}

		public void Explore(Shroud s)
		{
			if (map.Bounds != s.map.Bounds)
				throw new ArgumentException("The map bounds of these shrouds do not match.", "s");

			for (var y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
			{
				var rowIndex = y * stride;
				for (var x = map.Bounds.Left; x < map.Bounds.Right; x++)
				{
					var index = rowIndex + x;
					if (s.explored[index])
						explored[index] = true;
				}
			}

			ExploredBounds = Rectangle.Union(ExploredBounds, s.ExploredBounds);
		}

		public void ExploreAll(World world)
		{
			if (map.Bounds != world.Map.Bounds)
				throw new ArgumentException("The map bounds of the world does not match this shroud.", "world");

			for (var y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
			{
				var rowIndex = y * stride;
				for (var x = map.Bounds.Left; x < map.Bounds.Right; x++)
					explored[rowIndex + x] = true;
			}

			ExploredBounds = world.Map.Bounds;

			Invalidate();
		}

		public void ResetExploration()
		{
			for (var y = map.Bounds.Top; y < map.Bounds.Bottom; y++)
			{
				var rowIndex = y * stride;
				for (var x = map.Bounds.Left; x < map.Bounds.Right; x++)
				{
					var index = rowIndex + x;
					explored[index] = visibleCount[index] > 0;
				}
			}

			Invalidate();
		}

		public bool IsExplored(CPos xy) { return IsExplored(xy.X, xy.Y); }
		public bool IsExplored(int x, int y)
		{
			if (!map.IsInMap(x, y))
				return false;

			if (Disabled || !self.World.LobbyInfo.GlobalSettings.Shroud)
				return true;

			var index = PosToIndex(x, y);
			return explored[index] && (generatedShroudCount[index] == 0 || visibleCount[index] > 0);
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

			var index = PosToIndex(x, y);
			return visibleCount[index] > 0;
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
