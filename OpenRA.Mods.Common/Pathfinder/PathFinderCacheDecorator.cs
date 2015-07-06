#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Pathfinder
{
	/// <summary>
	/// A decorator used to cache the pathfinder (Decorator design pattern)
	/// </summary>
	public class PathFinderCacheDecorator : IPathFinder
	{
		readonly IPathFinder pathFinder;
		readonly ICacheStorage<List<CPos>> cacheStorage;

		public PathFinderCacheDecorator(IPathFinder pathFinder, ICacheStorage<List<CPos>> cacheStorage)
		{
			this.pathFinder = pathFinder;
			this.cacheStorage = cacheStorage;
		}

		public List<CPos> FindUnitPath(CPos source, CPos target, Actor self)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindUnitPath" + self.ActorID + source.X + source.Y + target.X + target.Y;
				var cachedPath = cacheStorage.Retrieve(key);

				if (cachedPath != null)
					return cachedPath;

				var pb = pathFinder.FindUnitPath(source, target, self);

				cacheStorage.Store(key, pb);

				return pb;
			}
		}

		public List<CPos> FindUnitPathToRange(CPos source, SubCell srcSub, WPos target, WDist range, Actor self)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindUnitPathToRange" + self.ActorID + source.X + source.Y + target.X + target.Y;
				var cachedPath = cacheStorage.Retrieve(key);

				if (cachedPath != null)
					return cachedPath;

				var pb = pathFinder.FindUnitPathToRange(source, srcSub, target, range, self);

				cacheStorage.Store(key, pb);

				return pb;
			}
		}

		public List<CPos> FindPath(IPathSearch search)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindPath" + search.Id;
				var cachedPath = cacheStorage.Retrieve(key);

				if (cachedPath != null)
					return cachedPath;

				var pb = pathFinder.FindPath(search);

				cacheStorage.Store(key, pb);

				return pb;
			}
		}

		public List<CPos> FindBidiPath(IPathSearch fromSrc, IPathSearch fromDest)
		{
			using (new PerfSample("Pathfinder"))
			{
				var key = "FindBidiPath" + fromSrc.Id + fromDest.Id;
				var cachedPath = cacheStorage.Retrieve(key);

				if (cachedPath != null)
					return cachedPath;

				var pb = pathFinder.FindBidiPath(fromSrc, fromDest);

				cacheStorage.Store(key, pb);

				return pb;
			}
		}
	}
}
