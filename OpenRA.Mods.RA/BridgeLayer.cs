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
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BridgeLayerInfo : TraitInfo<BridgeLayer> { }

	class BridgeLayer : ILoadWorldHook, ICustomTerrain
	{
		// for tricky things like bridges.
		Bridge[,] customTerrain;

		void MakeBridges(World w)
		{
			var tl = w.Map.TopLeft;
			var br = w.Map.BottomRight;
			
			for (int i = tl.X; i < br.X; i++)
				for (int j = tl.Y; j < br.Y; j++)
					if (IsBridge(w, w.Map.MapTiles[i, j].type))
						ConvertBridgeToActor(w, i, j);

			foreach (var b in w.Actors.SelectMany(a => a.traits.WithInterface<Bridge>()))
				b.FinalizeBridges(w, customTerrain);
		}
		
		void ConvertBridgeToActor(World w, int i, int j)
		{
			Log.Write("Converting bridge at {0} {1}",i,j);
			
			var tile = w.Map.MapTiles[i, j].type;
			var image = w.Map.MapTiles[i, j].image;
			var template = w.TileSet.walk[tile];

			// base position of the tile
			var ni = i - image % template.Size.X;
			var nj = j - image / template.Size.X;

			var replacedTiles = new Dictionary<int2, int>();
			for (var x = ni; x < ni + template.Size.X; x++)
				for (var y = nj; y < nj + template.Size.Y; y++)
				{
					var n = (x - ni) + template.Size.X * (y - nj);
					if (!template.TerrainType.ContainsKey(n)) continue;

					if (w.Map.IsInMap(x, y))
						if (w.Map.MapTiles[x, y].type == tile
							&& w.Map.MapTiles[x, y].index == n)
						{
							// stash it
							replacedTiles[new int2(x, y)] = w.Map.MapTiles[x, y].index;
							// remove the tile from the actual map
							w.Map.MapTiles[x, y].type = 0xfffe;
							w.Map.MapTiles[x, y].index = 0;
							w.Map.MapTiles[x, y].image = 0;
						}
				}

			if (replacedTiles.Any())
			{
				var a = w.CreateActor(template.Bridge, new int2(ni, nj), w.NeutralPlayer);
				var br = a.traits.Get<Bridge>();
				
				foreach (var t in replacedTiles.Keys)
					customTerrain[t.X, t.Y] = br;
				
				br.SetTiles(w, template, replacedTiles);
			}
		}
		
		public float GetCost(int2 p, UnitMovementType umt)
		{
			if (customTerrain[p.X, p.Y] != null)
				return customTerrain[p.X,p.Y].GetCost(p,umt);
			return 1f;
		}
		public float GetSpeedModifier(int2 p, UnitMovementType umt)
		{
			if (customTerrain[p.X, p.Y] != null)
				return customTerrain[p.X,p.Y].GetSpeedModifier(p,umt);
			return 1f;
		}
		
		static bool IsBridge(World w, ushort t)
		{
			return w.TileSet.walk[t].Bridge != null;
		}

		public void WorldLoaded(World w)
		{
			customTerrain = new Bridge[w.Map.MapSize.X, w.Map.MapSize.Y];
			MakeBridges(w);
		}
	}
}
