using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Traits
{
	class WallLoadHookInfo : ITraitInfo
	{
		public readonly string[] OverlayTypes = { };
		public readonly string ActorType = "brik";

		public object Create(Actor self) { return new WallLoadHook( self, this ); }
	}

	class WallLoadHook : IGameStarted
	{
		WallLoadHookInfo info;
		public WallLoadHook(Actor self, WallLoadHookInfo info) { this.info = info; }

		public void GameStarted(World w)
		{
			var map = w.Map;

			for (int y = map.YOffset; y < map.YOffset + map.Height; y++)
				for (int x = map.XOffset; x < map.XOffset + map.Width; x++)
					if (info.OverlayTypes.Contains(w.Map.MapTiles[x, y].overlay))
						w.CreateActor(info.ActorType, new int2(x, y), w.NeutralPlayer);
		}
	}
}
