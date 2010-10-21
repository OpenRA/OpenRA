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
using System.Diagnostics;
using System.Linq;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Move
{
	public class PathFinderInfo : ITraitInfo
	{
		public object Create( ActorInitializer init ) { return new PathFinder( init.world ); }
	}

	public class PathFinder
	{
		readonly World world;
		public PathFinder( World world ) { this.world = world; }

		class CachedPath
		{
			public int2 from;
			public int2 to;
			public List<int2> result;
			public int tick;
			public Actor actor;
		}

		List<CachedPath> CachedPaths = new List<CachedPath>();
		const int MaxPathAge = 50;	/* x 40ms ticks */

		public List<int2> FindUnitPath(int2 from, int2 target, Actor self)
		{
			using (new PerfSample("Pathfinder"))
			{
				var cached = CachedPaths.FirstOrDefault(p => p.from == from && p.to == target && p.actor == self);
				if (cached != null)
				{
					Log.Write("debug", "Actor {0} asked for a path from {1} tick(s) ago", self.ActorID, world.FrameNumber - cached.tick);
					cached.tick = world.FrameNumber;
					return new List<int2>(cached.result);
				}
				
				var mi = self.Info.Traits.Get<MobileInfo>();

				var pb = FindBidiPath(
					PathSearch.FromPoint(world, mi, target, from, true)
						.WithCustomBlocker(AvoidUnitsNear(from, 4, self)),
					PathSearch.FromPoint(world, mi, from, target, true)
						.WithCustomBlocker(AvoidUnitsNear(from, 4, self))
						.InReverse());

				CheckSanePath2(pb, from, target);

				CachedPaths.RemoveAll(p => world.FrameNumber - p.tick > MaxPathAge);
				CachedPaths.Add(new CachedPath { from = from, to = target, actor = self, result = pb, tick = world.FrameNumber });
				return new List<int2>(pb);
			}
		}

		public List<int2> FindUnitPathToRange( int2 src, int2 target, int range, Actor self )
		{
			using( new PerfSample( "Pathfinder" ) )
			{
				var mobileInfo = self.Info.Traits.Get<MobileInfo>();
				var tilesInRange = world.FindTilesInCircle(target, range)
					.Where( t => Mobile.CanEnterCell(self.World, mobileInfo, t, null, true));

				var path = FindPath( PathSearch.FromPoints( world, mobileInfo, tilesInRange, src, false )
					.WithCustomBlocker(AvoidUnitsNear(src, 4, self))
					.InReverse());
				path.Reverse();
				return path;
			}
		}
		
		public Func<int2, bool> AvoidUnitsNear(int2 p, int dist, Actor self)
		{
			return q =>
				p != q &&
				((p - q).LengthSquared < dist * dist) &&
				(world.WorldActor.Trait<UnitInfluence>().GetUnitsAt(q).Any(a => a.Group != self.Group));
		}

		public List<int2> FindPath( PathSearch search )
		{
			using (new PerfSample("Pathfinder"))
			{
				while (!search.queue.Empty)
				{
					var p = search.Expand( world );
					if (search.heuristic(p) == 0)
						return MakePath(search.cellInfo, p);
				}

				// no path exists
				return new List<int2>();
			}
		}

		static List<int2> MakePath( CellInfo[ , ] cellInfo, int2 destination )
		{
			List<int2> ret = new List<int2>();
			int2 pathNode = destination;

			while( cellInfo[ pathNode.X, pathNode.Y ].Path != pathNode )
			{
				ret.Add( pathNode );
				pathNode = cellInfo[ pathNode.X, pathNode.Y ].Path;
			}

			ret.Add(pathNode);
			CheckSanePath(ret);
			return ret;
		}



		public List<int2> FindBidiPath(			/* searches from both ends toward each other */
			PathSearch fromSrc,
			PathSearch fromDest)
		{
			using (new PerfSample("Pathfinder"))
			{
				while (!fromSrc.queue.Empty && !fromDest.queue.Empty)
				{
					/* make some progress on the first search */
					var p = fromSrc.Expand( world );

					if (fromDest.cellInfo[p.X, p.Y].Seen && fromDest.cellInfo[p.X, p.Y].MinCost < float.PositiveInfinity)
						return MakeBidiPath(fromSrc, fromDest, p);

					/* make some progress on the second search */
					var q = fromDest.Expand( world );

					if (fromSrc.cellInfo[q.X, q.Y].Seen && fromSrc.cellInfo[q.X, q.Y].MinCost < float.PositiveInfinity)
						return MakeBidiPath(fromSrc, fromDest, q);
				}

				return new List<int2>();
			}
		}

		static List<int2> MakeBidiPath(PathSearch a, PathSearch b, int2 p)
		{
			var ca = a.cellInfo;
			var cb = b.cellInfo;

			var ret = new List<int2>();

			var q = p;
			while (ca[q.X, q.Y].Path != q)
			{
				ret.Add( q );
				q = ca[ q.X, q.Y ].Path;
			}
			ret.Add(q);

			ret.Reverse();

			q = p;
			while (cb[q.X, q.Y].Path != q)
			{
				q = cb[q.X, q.Y].Path;
				ret.Add(q);
			}

			CheckSanePath( ret );
			return ret;
		}

		[Conditional( "SANITY_CHECKS" )]
		static void CheckSanePath( List<int2> path )
		{
			if( path.Count == 0 )
				return;
			var prev = path[ 0 ];
			for( int i = 0 ; i < path.Count ; i++ )
			{
				var d = path[ i ] - prev;
				if( Math.Abs( d.X ) > 1 || Math.Abs( d.Y ) > 1 )
					throw new InvalidOperationException( "(PathFinder) path sanity check failed" );
				prev = path[ i ];
			}
		}

		[Conditional("SANITY_CHECKS")]
		static void CheckSanePath2(List<int2> path, int2 src, int2 dest)
		{
			if (path.Count == 0)
				return;

			if (path[0] != dest)
				throw new InvalidOperationException("(PathFinder) sanity check failed: doesn't go to dest");
			if (path[path.Count - 1] != src)
				throw new InvalidOperationException("(PathFinder) sanity check failed: doesn't come from src");
		}
	}

	public struct CellInfo
	{
		public int MinCost;
		public int2 Path;
		public bool Seen;

		public CellInfo( int minCost, int2 path, bool seen )
		{
			MinCost = minCost;
			Path = path;
			Seen = seen;
		}
	}

	public struct PathDistance : IComparable<PathDistance>
	{
		public int EstTotal;
		public int2 Location;

		public PathDistance(int estTotal, int2 location)
		{
			EstTotal = estTotal;
			Location = location;
		}

		public int CompareTo(PathDistance other)
		{
			return Math.Sign(EstTotal - other.EstTotal);
		}
	}
}
