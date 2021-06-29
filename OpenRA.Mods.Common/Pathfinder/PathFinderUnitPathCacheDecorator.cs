#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// A decorator used to cache FindUnitPath and FindUnitPathToRange (Decorator design pattern)
	/// </summary>
	public class PathFinderUnitPathCacheDecorator : IPathFinder
	{
		readonly IPathFinder pathFinder;
		readonly ICacheStorage<List<CPos>> cacheStorage;

		public PathFinderUnitPathCacheDecorator(IPathFinder pathFinder, ICacheStorage<List<CPos>> cacheStorage)
		{
			this.pathFinder = pathFinder;
			this.cacheStorage = cacheStorage;
		}

		public List<CPos> FindUnitPath(CPos source, CPos target, Actor self, Actor ignoreActor, BlockedByActor check)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindUnitPath" + self.ActorID + source.X + source.Y + target.X + target.Y;

				// Only cache path when transient actors are ignored, otherwise there is no guarantee that the path
				// is still valid at the next check.
				if (check == BlockedByActor.None)
				{
					var cachedPath = cacheStorage.Retrieve(key);
					if (cachedPath != null)
						return cachedPath;
				}

				var pb = pathFinder.FindUnitPath(source, target, self, ignoreActor, check);

				if (check == BlockedByActor.None)
					cacheStorage.Store(key, pb);

				return pb;
			}
		}

		public List<CPos> FindUnitPathToRange(CPos source, SubCell srcSub, WPos target, WDist range, Actor self, BlockedByActor check)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindUnitPathToRange" + self.ActorID + source.X + source.Y + target.X + target.Y;

				if (check == BlockedByActor.None)
				{
					var cachedPath = cacheStorage.Retrieve(key);
					if (cachedPath != null)
						return cachedPath;
				}

				var pb = pathFinder.FindUnitPathToRange(source, srcSub, target, range, self, check);

				if (check == BlockedByActor.None)
					cacheStorage.Store(key, pb);

				return pb;
			}
		}

		public List<CPos> FindPath(IPathSearch search)
		{
			using (new PerfSample("Pathfinder"))
				return pathFinder.FindPath(search);
		}

		public List<CPos> FindBidiPath(IPathSearch fromSrc, IPathSearch fromDest)
		{
			using (new PerfSample("Pathfinder"))
				return pathFinder.FindBidiPath(fromSrc, fromDest);
		}
	}
}
