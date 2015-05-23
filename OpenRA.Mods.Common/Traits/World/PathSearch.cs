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
using System.Drawing;
using System.Linq;
using OpenRA;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.Traits
{
	public sealed class PathSearch : IDisposable
	{
		public CellLayer<CellInfo> CellInfo;
		public PriorityQueue<PathDistance> Queue;
		public Func<CPos, int> Heuristic;
		public bool CheckForBlocked;
		public Actor IgnoredActor;
		public bool InReverse;
		public HashSet<CPos> Considered;
		public Player Owner { get { return self.Owner; } }
		public int MaxCost;

		Actor self;
		MobileInfo mobileInfo;
		Func<CPos, int> customCost;
		Func<CPos, bool> customBlock;
		int laneBias = 1;

		public PathSearch(World world, MobileInfo mobileInfo, Actor self)
		{
			this.self = self;
			CellInfo = InitCellInfo();
			this.mobileInfo = mobileInfo;
			this.self = self;
			customCost = null;
			Queue = new PriorityQueue<PathDistance>();
			Considered = new HashSet<CPos>();
			MaxCost = 0;
		}

		public static PathSearch Search(World world, MobileInfo mi, Actor self, bool checkForBlocked)
		{
			var search = new PathSearch(world, mi, self)
			{
				CheckForBlocked = checkForBlocked
			};

			return search;
		}

		public static PathSearch FromPoint(World world, MobileInfo mi, Actor self, CPos from, CPos target, bool checkForBlocked)
		{
			var search = new PathSearch(world, mi, self)
			{
				Heuristic = DefaultEstimator(target),
				CheckForBlocked = checkForBlocked
			};

			search.AddInitialCell(from);
			return search;
		}

		public static PathSearch FromPoints(World world, MobileInfo mi, Actor self, IEnumerable<CPos> froms, CPos target, bool checkForBlocked)
		{
			var search = new PathSearch(world, mi, self)
			{
				Heuristic = DefaultEstimator(target),
				CheckForBlocked = checkForBlocked
			};

			foreach (var sl in froms)
				search.AddInitialCell(sl);

			return search;
		}

		public static Func<CPos, int> DefaultEstimator(CPos destination)
		{
			return here =>
			{
				var diag = Math.Min(Math.Abs(here.X - destination.X), Math.Abs(here.Y - destination.Y));
				var straight = Math.Abs(here.X - destination.X) + Math.Abs(here.Y - destination.Y);

				// HACK: this relies on fp and cell-size assumptions.
				var h = (3400 * diag / 24) + 100 * (straight - (2 * diag));
				return (int)(h * 1.001);
			};
		}

		public PathSearch Reverse()
		{
			InReverse = true;
			return this;
		}

		public PathSearch WithCustomBlocker(Func<CPos, bool> customBlock)
		{
			this.customBlock = customBlock;
			return this;
		}

		public PathSearch WithIgnoredActor(Actor b)
		{
			IgnoredActor = b;
			return this;
		}

		public PathSearch WithHeuristic(Func<CPos, int> h)
		{
			Heuristic = h;
			return this;
		}

		public PathSearch WithCustomCost(Func<CPos, int> w)
		{
			customCost = w;
			return this;
		}

		public PathSearch WithoutLaneBias()
		{
			laneBias = 0;
			return this;
		}

		public PathSearch FromPoint(CPos from)
		{
			AddInitialCell(from);
			return this;
		}

		// Sets of neighbors for each incoming direction. These exclude the neighbors which are guaranteed
		// to be reached more cheaply by a path through our parent cell which does not include the current cell.
		// For horizontal/vertical directions, the set is the three cells 'ahead'. For diagonal directions, the set
		// is the three cells ahead, plus the two cells to the side, which we cannot exclude without knowing if
		// the cell directly between them and our parent is passable.
		static CVec[][] directedNeighbors = {
			new CVec[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1), new CVec(-1, 0), new CVec(-1, 1) },
			new CVec[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1) },
			new CVec[] { new CVec(-1, -1), new CVec(0, -1), new CVec(1, -1), new CVec(1, 0), new CVec(1, 1) },
			new CVec[] { new CVec(-1, -1), new CVec(-1, 0), new CVec(-1, 1) },
			CVec.Directions,
			new CVec[] { new CVec(1, -1), new CVec(1, 0), new CVec(1, 1) },
			new CVec[] { new CVec(-1, -1), new CVec(-1, 0), new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) },
			new CVec[] { new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) },
			new CVec[] { new CVec(1, -1), new CVec(1, 0), new CVec(-1, 1), new CVec(0, 1), new CVec(1, 1) },
		};

		static CVec[] GetNeighbors(CPos p, CPos prev)
		{
			var dx = p.X - prev.X;
			var dy = p.Y - prev.Y;
			var index = dy * 3 + dx + 4;

			return directedNeighbors[index];
		}

		public CPos Expand(World world)
		{
			var p = Queue.Pop();
			while (CellInfo[p.Location].Seen)
			{
				if (Queue.Empty)
					return p.Location;

				p = Queue.Pop();
			}

			var pCell = CellInfo[p.Location];
			pCell.Seen = true;
			CellInfo[p.Location] = pCell;

			var thisCost = mobileInfo.MovementCostForCell(world, p.Location);

			if (thisCost == int.MaxValue)
				return p.Location;

			if (customCost != null)
			{
				var c = customCost(p.Location);
				if (c == int.MaxValue)
					return p.Location;
			}

			// This current cell is ok; check useful immediate directions:
			Considered.Add(p.Location);

			var directions = GetNeighbors(p.Location, pCell.Path);

			for (var i = 0; i < directions.Length; ++i)
			{
				var d = directions[i];

				var newHere = p.Location + d;

				// Is this direction flat-out unusable or already seen?
				if (!world.Map.Contains(newHere))
					continue;

				if (CellInfo[newHere].Seen)
					continue;

				// Now we may seriously consider this direction using heuristics:
				var costHere = mobileInfo.MovementCostForCell(world, newHere);

				if (costHere == int.MaxValue)
					continue;

				if (!mobileInfo.CanEnterCell(world, self, newHere, IgnoredActor, CheckForBlocked ? CellConditions.TransientActors : CellConditions.None))
					continue;

				if (customBlock != null && customBlock(newHere))
					continue;

				var est = Heuristic(newHere);
				if (est == int.MaxValue)
					continue;

				var cellCost = costHere;
				if (d.X * d.Y != 0)
					cellCost = (cellCost * 34) / 24;

				var userCost = 0;
				if (customCost != null)
				{
					userCost = customCost(newHere);
					cellCost += userCost;
				}

				// directional bonuses for smoother flow!
				if (laneBias != 0)
				{
					var ux = newHere.X + (InReverse ? 1 : 0) & 1;
					var uy = newHere.Y + (InReverse ? 1 : 0) & 1;

					if (ux == 0 && d.Y < 0)
						cellCost += laneBias;
					else if (ux == 1 && d.Y > 0)
						cellCost += laneBias;

					if (uy == 0 && d.X < 0)
						cellCost += laneBias;
					else if (uy == 1 && d.X > 0)
						cellCost += laneBias;
				}

				var newCost = CellInfo[p.Location].MinCost + cellCost;

				// Cost is even higher; next direction:
				if (newCost > CellInfo[newHere].MinCost)
					continue;

				var hereCell = CellInfo[newHere];
				hereCell.Path = p.Location;
				hereCell.MinCost = newCost;
				CellInfo[newHere] = hereCell;

				Queue.Add(new PathDistance(newCost + est, newHere));

				if (newCost > MaxCost)
					MaxCost = newCost;

				Considered.Add(newHere);
			}

			return p.Location;
		}

		public void AddInitialCell(CPos location)
		{
			if (!self.World.Map.Contains(location))
				return;

			CellInfo[location] = new CellInfo(0, location, false);
			Queue.Add(new PathDistance(Heuristic(location), location));
		}

		static readonly Queue<CellLayer<CellInfo>> CellInfoPool = new Queue<CellLayer<CellInfo>>();
		static readonly object DefaultCellInfoLayerSync = new object();
		static CellLayer<CellInfo> defaultCellInfoLayer;

		static CellLayer<CellInfo> GetFromPool()
		{
			lock (CellInfoPool)
				return CellInfoPool.Dequeue();
		}

		static void PutBackIntoPool(CellLayer<CellInfo> ci)
		{
			lock (CellInfoPool)
				CellInfoPool.Enqueue(ci);
		}

		CellLayer<CellInfo> InitCellInfo()
		{
			CellLayer<CellInfo> result = null;
			var map = self.World.Map;
			var mapSize = new Size(map.MapSize.X, map.MapSize.Y);

			// HACK: Uses a static cache so that double-ended searches (which have two PathSearch instances)
			// can implicitly share data.  The PathFinder should allocate the CellInfo array and pass it
			// explicitly to the things that need to share it.
			while (CellInfoPool.Count > 0)
			{
				var cellInfo = GetFromPool();
				if (cellInfo.Size != mapSize || cellInfo.Shape != map.TileShape)
				{
					Log.Write("debug", "Discarding old pooled CellInfo of wrong size.");
					continue;
				}

				result = cellInfo;
				break;
			}

			if (result == null)
				result = new CellLayer<CellInfo>(map);

			lock (DefaultCellInfoLayerSync)
			{
				if (defaultCellInfoLayer == null ||
					defaultCellInfoLayer.Size != mapSize ||
					defaultCellInfoLayer.Shape != map.TileShape)
				{
					defaultCellInfoLayer = new CellLayer<CellInfo>(map);
					for (var v = 0; v < mapSize.Height; v++)
						for (var u = 0; u < mapSize.Width; u++)
							defaultCellInfoLayer[u, v] = new CellInfo(int.MaxValue, Map.MapToCell(map.TileShape, new CPos(u, v)), false);
				}

				result.CopyValuesFrom(defaultCellInfoLayer);
			}

			return result;
		}

		bool disposed;
		public void Dispose()
		{
			if (disposed)
				return;

			disposed = true;

			PutBackIntoPool(CellInfo);
			CellInfo = null;

			GC.SuppressFinalize(this);
		}

		~PathSearch() { Dispose(); }
	}
}
