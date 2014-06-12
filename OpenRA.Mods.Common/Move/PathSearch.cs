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
using System.Linq;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Move
{
	public sealed class PathSearch : IDisposable
	{
		World world;
		public CellInfo[,] cellInfo;
		public PriorityQueue<PathDistance> queue;
		public Func<CPos, int> heuristic;
		Func<CPos, int> customCost;
		Func<CPos, bool> customBlock;
		public bool checkForBlocked;
		public Actor ignoreBuilding;
		public bool inReverse;
		public HashSet<CPos> considered;
		public int maxCost;
		Pair<CVec, int>[] nextDirections;
		MobileInfo mobileInfo;
		Actor self;
		public Player owner { get { return self.Owner; } }

		public PathSearch(World world, MobileInfo mobileInfo, Actor self)
		{
			this.world = world;
			cellInfo = InitCellInfo();
			this.mobileInfo = mobileInfo;
			this.self = self;
			customCost = null;
			queue = new PriorityQueue<PathDistance>();
			considered = new HashSet<CPos>();
			maxCost = 0;
			nextDirections = CVec.directions.Select(d => new Pair<CVec, int>(d, 0)).ToArray();
		}

		public PathSearch InReverse()
		{
			inReverse = true;
			return this;
		}

		public PathSearch WithCustomBlocker(Func<CPos, bool> customBlock)
		{
			this.customBlock = customBlock;
			return this;
		}

		public PathSearch WithIgnoredBuilding(Actor b)
		{
			ignoreBuilding = b;
			return this;
		}

		public PathSearch WithHeuristic(Func<CPos, int> h)
		{
			heuristic = h;
			return this;
		}

		public PathSearch WithCustomCost(Func<CPos, int> w)
		{
			customCost = w;
			return this;
		}

		public PathSearch WithoutLaneBias()
		{
			LaneBias = 0;
			return this;
		}

		public PathSearch FromPoint(CPos from)
		{
			AddInitialCell(from);
			return this;
		}

		int LaneBias = 1;

		public CPos Expand(World world)
		{
			var p = queue.Pop();
			while (cellInfo[p.Location.X, p.Location.Y].Seen)
				if (queue.Empty)
					return p.Location;
				else
					p = queue.Pop();

			cellInfo[p.Location.X, p.Location.Y].Seen = true;

			var thisCost = mobileInfo.MovementCostForCell(world, p.Location);

			if (thisCost == int.MaxValue)
				return p.Location;

			if (customCost != null)
			{
				int c = customCost(p.Location);
				if (c == int.MaxValue)
					return p.Location;
			}

			// This current cell is ok; check all immediate directions:
			considered.Add(p.Location);

			for (int i = 0; i < nextDirections.Length; ++i)
			{
				CVec d = nextDirections[i].First;
				nextDirections[i].Second = int.MaxValue;

				CPos newHere = p.Location + d;

				// Is this direction flat-out unusable or already seen?
				if (!world.Map.IsInMap(newHere.X, newHere.Y))
					continue;
				if (cellInfo[newHere.X, newHere.Y].Seen)
					continue;

				// Now we may seriously consider this direction using heuristics:
				var costHere = mobileInfo.MovementCostForCell(world, newHere);

				if (costHere == int.MaxValue)
					continue;

				if (!mobileInfo.CanEnterCell(world, self, newHere, ignoreBuilding, checkForBlocked, false))
					continue;

				if (customBlock != null && customBlock(newHere))
					continue;

				var est = heuristic(newHere);
				if (est == int.MaxValue)
					continue;

				int cellCost = costHere;
				if (d.X * d.Y != 0) cellCost = (cellCost * 34) / 24;

				int userCost = 0;
				if (customCost != null)
				{
					userCost = customCost(newHere);
					cellCost += userCost;
				}

				// directional bonuses for smoother flow!
				if (LaneBias != 0)
				{
					var ux = (newHere.X + (inReverse ? 1 : 0) & 1);
					var uy = (newHere.Y + (inReverse ? 1 : 0) & 1);

					if (ux == 0 && d.Y < 0) cellCost += LaneBias;
					else if (ux == 1 && d.Y > 0) cellCost += LaneBias;
					if (uy == 0 && d.X < 0) cellCost += LaneBias;
					else if (uy == 1 && d.X > 0) cellCost += LaneBias;
				}

				int newCost = cellInfo[p.Location.X, p.Location.Y].MinCost + cellCost;

				// Cost is even higher; next direction:
				if (newCost > cellInfo[newHere.X, newHere.Y].MinCost)
					continue;

				cellInfo[newHere.X, newHere.Y].Path = p.Location;
				cellInfo[newHere.X, newHere.Y].MinCost = newCost;

				nextDirections[i].Second = newCost + est;
				queue.Add(new PathDistance(newCost + est, newHere));

				if (newCost > maxCost) maxCost = newCost;
				considered.Add(newHere);
			}

			// Sort to prefer the cheaper direction:
			//Array.Sort(nextDirections, (a, b) => a.Second.CompareTo(b.Second));

			return p.Location;
		}

		public void AddInitialCell(CPos location)
		{
			if (!world.Map.IsInMap(location.X, location.Y))
				return;

			cellInfo[location.X, location.Y] = new CellInfo(0, location, false);
			queue.Add(new PathDistance(heuristic(location), location));
		}

		public static PathSearch Search(World world, MobileInfo mi, Actor self, bool checkForBlocked)
		{
			var search = new PathSearch(world, mi, self)
			{
				checkForBlocked = checkForBlocked
			};
			return search;
		}

		public static PathSearch FromPoint(World world, MobileInfo mi, Actor self, CPos from, CPos target, bool checkForBlocked)
		{
			var search = new PathSearch(world, mi, self)
			{
				heuristic = DefaultEstimator(target),
				checkForBlocked = checkForBlocked
			};

			search.AddInitialCell(from);
			return search;
		}

		public static PathSearch FromPoints(World world, MobileInfo mi, Actor self, IEnumerable<CPos> froms, CPos target, bool checkForBlocked)
		{
			var search = new PathSearch(world, mi, self)
			{
				heuristic = DefaultEstimator(target),
				checkForBlocked = checkForBlocked
			};

			foreach (var sl in froms)
				search.AddInitialCell(sl);

			return search;
		}

		static readonly Queue<CellInfo[,]> cellInfoPool = new Queue<CellInfo[,]>();

		static CellInfo[,] GetFromPool()
		{
			lock (cellInfoPool)
				return cellInfoPool.Dequeue();
		}

		static void PutBackIntoPool(CellInfo[,] ci)
		{
			lock (cellInfoPool)
				cellInfoPool.Enqueue(ci);
		}

		CellInfo[,] InitCellInfo()
		{
			CellInfo[,] result = null;
			while (cellInfoPool.Count > 0)
			{
				var cellInfo = GetFromPool();
				if (cellInfo.GetUpperBound(0) != world.Map.MapSize.X - 1 ||
					cellInfo.GetUpperBound(1) != world.Map.MapSize.Y - 1)
				{
					Log.Write("debug", "Discarding old pooled CellInfo of wrong size.");
					continue;
				}

				result = cellInfo;
				break;
			}

			if (result == null)
				result = new CellInfo[world.Map.MapSize.X, world.Map.MapSize.Y];

			for (int x = 0; x < world.Map.MapSize.X; x++)
				for (int y = 0; y < world.Map.MapSize.Y; y++)
					result[ x, y ] = new CellInfo( int.MaxValue, new CPos( x, y ), false );

			return result;
		}

		public static Func<CPos, int> DefaultEstimator(CPos destination)
		{
			return here =>
			{
				int diag = Math.Min(Math.Abs(here.X - destination.X), Math.Abs(here.Y - destination.Y));
				int straight = (Math.Abs(here.X - destination.X) + Math.Abs(here.Y - destination.Y));
				int h = (3400 * diag / 24) + 100 * (straight - (2 * diag));
				h = (int)(h * 1.001);
				return h;
			};
		}

		bool disposed;
		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;

			PutBackIntoPool(cellInfo);
			cellInfo = null;

			GC.SuppressFinalize(this);
		}

		~PathSearch() { Dispose(); }
	}
}
