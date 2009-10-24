using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using IjwFramework.Types;
using IjwFramework.Collections;

namespace OpenRa.Game
{
	class BuildingInfluenceMap
	{
		Pair<Actor, float>[,] influence = new Pair<Actor, float>[128, 128];
		readonly int maxDistance;	/* clip limit for voronoi cells */
		static readonly Pair<Actor, float> NoClaim = Pair.New((Actor)null, float.MaxValue);

		public BuildingInfluenceMap(int maxDistance)
		{
			this.maxDistance = maxDistance;

			for (int j = 0; j < 128; j++)
				for (int i = 0; i < 128; i++)
					influence[i, j] = NoClaim;

			Game.world.ActorAdded +=
				a => { if (a.traits.Contains<Traits.Building>()) AddInfluence(a); };
			Game.world.ActorRemoved +=
				a => { if (a.traits.Contains<Traits.Building>()) RemoveInfluence(a); };
		}

		void AddInfluence(Actor a)
		{
			var tiles = Footprint.UnpathableTiles(a.unitInfo, a.Location).ToArray();
			var min = int2.Max(new int2(0, 0), 
				tiles.Aggregate(int2.Min) - new int2(maxDistance, maxDistance));
			var max = int2.Min(new int2(128, 128), 
				tiles.Aggregate(int2.Max) + new int2(maxDistance, maxDistance));

			var pq = new PriorityQueue<Cell>();

			var initialTileCount = 0;

			foreach (var t in tiles)
				if (IsValid(t))
				{
					pq.Add(new Cell { location = t, distance = 0, actor=a });
					++initialTileCount;
				}

			Log.Write("Recalculating voronoi region for {{ {0} ({1},{2}) }}: {3} initial tiles",
				a.unitInfo.Name, a.Location.X, a.Location.Y, initialTileCount);

			var updatedCells = 0;

			while (!pq.Empty)
			{
				var c = pq.Pop();

				if (influence[c.location.X, c.location.Y].Second < c.distance)
					continue;

				influence[c.location.X, c.location.Y].First = c.actor;
				influence[c.location.X, c.location.Y].Second = c.distance;

				++updatedCells;

				if (c.distance + 1 > maxDistance) continue;

				foreach (var d in PathFinder.directions)
				{
					var e = c.location + d;
					if (e.X < min.X || e.Y < min.Y || e.X > max.X || e.Y > max.Y)
						continue;

					pq.Add(new Cell
					{
						location = e,
						distance = c.distance + ((d.X * d.Y != 0) ? 1.414f : 1f),
						actor = c.actor
					});
				}
			}

			Log.Write("Finished recalculating region. {0} cells updated.", updatedCells);
		}

		void RemoveInfluence(Actor a)
		{
			var tiles = Footprint.UnpathableTiles(a.unitInfo, a.Location).ToArray();
			var min = int2.Max(new int2(0, 0),
				tiles.Aggregate(int2.Min) - new int2(maxDistance, maxDistance));
			var max = int2.Min(new int2(128, 128),
				tiles.Aggregate(int2.Max) + new int2(maxDistance, maxDistance));
			
			for (var j = min.Y; j <= max.Y; j++)
				for (var i = min.X; i <= max.X; i++)
					if (influence[i, j].First == a)
						influence[i, j] = NoClaim;

			// slightly expanded bounds for collecting candidates for recalculation.
			var min2 = int2.Max(new int2(0, 0), min - new int2(1, 1));
			var max2 = int2.Min(new int2(128, 128), max + new int2(1, 1));

			var actors = new List<Actor>();
			for (var j = min2.Y; j <= max2.Y; j++)
				for (var i = min2.X; i <= max2.X; i++)
					if (influence[i, j].First != null && !actors.Contains(influence[i, j].First))
						actors.Add(influence[i, j].First);

			Log.Write("Finished collecting candidates for evacuated region = {0}", actors.Count);

			foreach (var b in actors)
				AddInfluence(b);	/* we can actually safely constrain this a bit more... */
		}

		bool IsValid(int2 t)
		{
			return !(t.X < 0 || t.Y < 0 || t.X >= 128 || t.Y >= 128);
		}

		public Actor GetBuildingAt(int2 cell)
		{
			if (!IsValid(cell) || influence[cell.X, cell.Y].Second != 0)
				return null;
			return influence[cell.X, cell.Y].First;
		}

		public Actor GetNearestBuilding(int2 cell)
		{
			if (!IsValid(cell)) return null;
			return influence[cell.X, cell.Y].First;
		}

		public int GetDistanceToBuilding(int2 cell)
		{
			if (!IsValid(cell)) return int.MaxValue;
			return (int)influence[cell.X, cell.Y].Second;
		}

		struct Cell : IComparable<Cell>
		{
			public int2 location;
			public float distance;
			public Actor actor;

			public int CompareTo(Cell other)
			{
				return distance.CompareTo(other.distance);
			}
		}
	}
}
