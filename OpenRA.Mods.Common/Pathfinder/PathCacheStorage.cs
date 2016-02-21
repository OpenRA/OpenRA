#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Pathfinder
{
	public class PathCacheStorage : ICacheStorage<List<CPos>>
	{
		class CachedPath
		{
			public List<CPos> Result;
			public int Tick;
		}

		const int MaxPathAge = 50;
		readonly World world;
		Dictionary<string, CachedPath> cachedPaths = new Dictionary<string, CachedPath>(100);

		public PathCacheStorage(World world)
		{
			this.world = world;
		}

		public void Remove(string key)
		{
			cachedPaths.Remove(key);
		}

		public void Store(string key, List<CPos> data)
		{
			// Eventually clean up the cachedPaths dictionary
			if (cachedPaths.Count >= 100)
				foreach (var cachedPath in cachedPaths.Where(p => IsExpired(p.Value)).ToList())
					cachedPaths.Remove(cachedPath.Key);

			cachedPaths.Add(key, new CachedPath
			{
				Tick = world.WorldTick,
				Result = data
			});
		}

		public List<CPos> Retrieve(string key)
		{
			CachedPath cached;
			if (cachedPaths.TryGetValue(key, out cached))
			{
				if (IsExpired(cached))
				{
					cachedPaths.Remove(key);
					return null;
				}

				return cached.Result;
			}

			return null;
		}

		bool IsExpired(CachedPath path)
		{
			return world.WorldTick - path.Tick > MaxPathAge;
		}
	}
}