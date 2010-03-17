#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Traits
{
	class BridgeLoadHookInfo : StatelessTraitInfo<BridgeLoadHook> { }

	class BridgeLoadHook : ILoadWorldHook
	{
		static void MakeBridges(World w)
		{
			var mini = w.Map.XOffset; var maxi = w.Map.XOffset + w.Map.Width;
			var minj = w.Map.YOffset; var maxj = w.Map.YOffset + w.Map.Height;

			for (var j = minj; j < maxj; j++)
				for (var i = mini; i < maxi; i++)
					if (IsBridge(w, w.Map.MapTiles[i, j].tile))
						ConvertBridgeToActor(w, i, j);

			foreach (var br in w.Actors.SelectMany(a => a.traits.WithInterface<Bridge>()))
				br.FinalizeBridges(w);
		}

		static void ConvertBridgeToActor(World w, int i, int j)
		{
			var tile = w.Map.MapTiles[i, j].tile;
			var image = w.Map.MapTiles[i, j].image;
			var template = w.TileSet.walk[tile];

			// base position of the tile
			var ni = i - image % template.Size.X;
			var nj = j - image / template.Size.X;

			var replacedTiles = new Dictionary<int2, int>();
			for (var y = nj; y < nj + template.Size.Y; y++)
				for (var x = ni; x < ni + template.Size.X; x++)
				{
					var n = (x - ni) + template.Size.X * (y - nj);
					if (!template.TerrainType.ContainsKey(n)) continue;

					if (w.Map.IsInMap(x, y))
						if (w.Map.MapTiles[x, y].tile == tile
							&& w.Map.MapTiles[x, y].image == n)
						{
							// stash it
							replacedTiles[new int2(x, y)] = w.Map.MapTiles[x, y].image;
							// remove the tile from the actual map
							w.Map.MapTiles[x, y].tile = 0xfffe;
							w.Map.MapTiles[x, y].image = 0;
						}
				}

			if (replacedTiles.Any())
			{
				var a = w.CreateActor(template.Bridge, new int2(ni, nj), null);
				var br = a.traits.Get<Bridge>();
				br.SetTiles(w, template, replacedTiles);
			}
		}

		static bool IsBridge(World w, ushort t)
		{
			return w.TileSet.walk[t].Bridge != null;
		}

		public void WorldLoaded(World w) { MakeBridges(w); }
	}
}
