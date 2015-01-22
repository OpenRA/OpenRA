using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Traits
{
	public class PathCacheStorage : ICacheStorage<List<CPos>>
	{
		class CachedPath
		{
			public List<CPos> Result;
			public int Tick;
		}

		private const int MaxPathAge = 50;
		private readonly IWorld world;
		Dictionary<string, CachedPath> cachedPaths = new Dictionary<string, CachedPath>();

		public PathCacheStorage(IWorld world)
		{
			this.world = world;
		}

		public void Remove(string key)
		{
			cachedPaths.Remove(key);
		}

		public void Store(string key, List<CPos> data)
		{
			foreach (var cachedPath in cachedPaths.Where(p => world.WorldTick - p.Value.Tick > MaxPathAge).ToList())
			{
				cachedPaths.Remove(cachedPath.Key);
			}

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
				if (world.WorldTick - cached.Tick > MaxPathAge)
					cachedPaths.Remove(key);
				return cached.Result;
			}

			return null;
		}
	}
}
