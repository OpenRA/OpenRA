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

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Pathfinder
{
	public readonly struct PathCacheKey : IEquatable<PathCacheKey>
	{
		readonly uint actorID;
		readonly int source;
		readonly int target;
		readonly int targetY;
		readonly int hash;

		// FindUnitPath result.
		public PathCacheKey(uint actorID, CPos source, CPos target)
		{
			this.actorID = actorID;
			this.source = source.Bits;
			this.target = target.Bits;
			targetY = int.MinValue;
			hash = HashCode.Combine(actorID, this.source, this.target);
		}

		// FindUnitPathToRange result.
		public PathCacheKey(uint actorID, CPos source, WPos target)
		{
			this.actorID = actorID;
			this.source = source.Bits;
			this.target = target.X;
			targetY = target.Y;
			hash = HashCode.Combine(actorID, this.source, this.target, targetY);
		}

		public static bool operator ==(PathCacheKey me, PathCacheKey other)
		{
			return me.hash == other.hash
				&& me.actorID == other.actorID
				&& me.source == other.source
				&& me.target == other.target
				&& me.targetY == other.targetY;
		}

		public static bool operator !=(PathCacheKey me, PathCacheKey other)
		{
			return !(me == other);
		}

		public override int GetHashCode() { return hash; }

		public bool Equals(PathCacheKey other) { return this == other; }

		public override bool Equals(object obj) { return obj is PathCacheKey && Equals((PathCacheKey)obj); }
	}

	public sealed class PathCacheStorage
	{
		class CachedPath
		{
			public List<CPos> Result;
			public int Tick;
		}

		const int MaxPathAge = 50;
		readonly World world;
		readonly Dictionary<PathCacheKey, CachedPath> cachedPaths = new Dictionary<PathCacheKey, CachedPath>(100);

		public PathCacheStorage(World world)
		{
			this.world = world;
		}

		public void Remove(in PathCacheKey key)
		{
			cachedPaths.Remove(key);
		}

		public void Store(in PathCacheKey key, List<CPos> data)
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

		public List<CPos> Retrieve(in PathCacheKey key)
		{
			if (cachedPaths.TryGetValue(key, out var cached))
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
