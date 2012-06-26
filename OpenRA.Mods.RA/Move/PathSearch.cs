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
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	public class PathSearch : IDisposable
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

		MobileInfo mobileInfo;
		Player owner;

		public PathSearch(World world, MobileInfo mobileInfo, Player owner)
		{
			this.world = world;
			cellInfo = InitCellInfo();
			this.mobileInfo = mobileInfo;
			this.owner = owner;
			customCost = null;
			queue = new PriorityQueue<PathDistance>();
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

			foreach( CVec d in directions )
			{
				CPos newHere = p.Location + d;

				if (!world.Map.IsInMap(newHere.X, newHere.Y)) continue;
				if (cellInfo[newHere.X, newHere.Y].Seen)
					continue;

				var costHere = mobileInfo.MovementCostForCell(world, newHere);

				if (costHere == int.MaxValue)
					continue;

				if (!mobileInfo.CanEnterCell(world, owner, newHere, ignoreBuilding, checkForBlocked))
					continue;

				if (customBlock != null && customBlock(newHere))
					continue;

				var est = heuristic(newHere);
				if (est == int.MaxValue)
					continue;

				int cellCost = costHere;
				if (d.X * d.Y != 0) cellCost = (cellCost * 34) / 24;

				if (customCost != null)
					cellCost += customCost(newHere);

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

				if (newCost >= cellInfo[newHere.X, newHere.Y].MinCost)
					continue;

				cellInfo[newHere.X, newHere.Y].Path = p.Location;
				cellInfo[newHere.X, newHere.Y].MinCost = newCost;

				queue.Add(new PathDistance(newCost + est, newHere));
			}
			return p.Location;
		}

		static readonly CVec[] directions =
		{
			new CVec( -1, -1 ),
			new CVec( -1,  0 ),
			new CVec( -1,  1 ),
			new CVec(  0, -1 ),
			new CVec(  0,  1 ),
			new CVec(  1, -1 ),
			new CVec(  1,  0 ),
			new CVec(  1,  1 ),
		};

		public void AddInitialCell(CPos location)
		{
			if (!world.Map.IsInMap(location.X, location.Y))
				return;

			cellInfo[location.X, location.Y] = new CellInfo(0, location, false);
			queue.Add(new PathDistance(heuristic(location), location));
		}

		public static PathSearch Search(World world, MobileInfo mi, Player owner, bool checkForBlocked)
		{
			var search = new PathSearch(world, mi, owner)
			{
				checkForBlocked = checkForBlocked
			};
			return search;
		}

		public static PathSearch FromPoint(World world, MobileInfo mi, Player owner, CPos from, CPos target, bool checkForBlocked)
		{
			var search = new PathSearch(world, mi, owner)
			{
				heuristic = DefaultEstimator(target),
				checkForBlocked = checkForBlocked
			};

			search.AddInitialCell(from);
			return search;
		}

		public static PathSearch FromPoints(World world, MobileInfo mi, Player owner, IEnumerable<CPos> froms, CPos target, bool checkForBlocked)
		{
			var search = new PathSearch(world, mi, owner)
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
				CVec d = (here - destination).Abs();
				int diag = Math.Min(d.X, d.Y);
				int straight = Math.Abs(d.X - d.Y);
				return (3400 * diag / 24) + (100 * straight);
			};
		}

		bool disposed;
		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;
			GC.SuppressFinalize(this);
			PutBackIntoPool(cellInfo);
			cellInfo = null;
		}

		~PathSearch() { Dispose(); }
	}
}
