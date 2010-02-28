using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class WallLoadHookInfo : ITraitInfo
	{
		public readonly int[] OverlayIndices = { };
		public readonly string ActorType = "brik";

		public object Create(Actor self) { return new WallLoadHook( self, this ); }
	}

	class WallLoadHook : ILoadWorldHook
	{
		WallLoadHookInfo info;
		public WallLoadHook(Actor self, WallLoadHookInfo info)
		{
			this.info = info;
		}

		public void WorldLoaded(World w)
		{
			var map = w.Map;

			for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
				for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
					if (info.OverlayIndices.Contains(w.Map.MapTiles[x, y].overlay))
						w.CreateActor(info.ActorType, new int2(x, y), w.players[0]);	// todo: neutral player or null?
		}
	}
}
