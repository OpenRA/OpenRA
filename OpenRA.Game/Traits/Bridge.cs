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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.GameRules;

namespace OpenRA.Traits
{
	class BridgeInfo : ITraitInfo
	{
		public readonly bool Long = false;
		public readonly bool UseAlternateNames = false;
		public readonly int[] NorthOffset = null;
		public readonly int[] SouthOffset = null;
		public object Create(Actor self) { return new Bridge(self); }
	}

	class Bridge: IRender, INotifyDamage
	{
		Dictionary<int2, int> Tiles;
		List<Dictionary<int2, Sprite>> TileSprites = new List<Dictionary<int2,Sprite>>();
		List<TileTemplate> Templates = new List<TileTemplate>();
		Actor self;
		int state;
		Bridge northNeighbour, southNeighbour;

		public Bridge(Actor self) { this.self = self; self.RemoveOnDeath = false; }
		
		static string cachedTileset;
		static Cache<TileReference<ushort,byte>, Sprite> sprites;

		public IEnumerable<Renderable> Render(Actor self)
		{
			foreach (var t in TileSprites[state])
				yield return new Renderable(t.Value, Game.CellSize * t.Key, "terrain");
		}

		public void FinalizeBridges(World world, Bridge[,] bridges)
		{
			// go looking for our neighbors, if this is a long bridge.
			var info = self.Info.Traits.Get<BridgeInfo>();
			if (info.NorthOffset != null)
				northNeighbour = GetNeighbor(world, info.NorthOffset, bridges);
			if (info.SouthOffset != null)
				southNeighbour = GetNeighbor(world, info.SouthOffset, bridges);
		}
		
		public Bridge GetNeighbor(World world, int[] offset, Bridge[,] bridges)
		{
			if (offset == null) return null;
			var pos = self.Location + new int2(offset[0], offset[1]);
			if (!world.Map.IsInMap(pos.X, pos.Y)) return null;
			return bridges[pos.X, pos.Y];
		}
		
		
		public int StateFromTemplate(TileTemplate t)
		{
			var info = self.Info.Traits.Get<BridgeInfo>();
			if (info.UseAlternateNames)
			{
				if (t.Name.EndsWith("d")) return 2;
				if (t.Name.EndsWith("h")) return 1;
				return 0;
			}
			else
				return t.Name[t.Name.Length - 1] - 'a';
		}

		public string NameFromState(TileTemplate t, int state)
		{
			var info = self.Info.Traits.Get<BridgeInfo>();
			if (info.UseAlternateNames)
				return t.Bridge + new[] { "", "h", "d" }[state];
			else
				return t.Bridge + (char)(state + 'a');
		}

		public void SetTiles(World world, TileTemplate template, Dictionary<int2, int> replacedTiles)
		{
			Tiles = replacedTiles;
			state = StateFromTemplate(template);
			
			if (cachedTileset != world.Map.Tileset)
			{
				cachedTileset = world.Map.Tileset;
				sprites = new Cache<TileReference<ushort,byte>, Sprite>(
				x => SheetBuilder.SharedInstance.Add(world.TileSet.GetBytes(x),
					new Size(Game.CellSize, Game.CellSize)));
			}

			var numStates = self.Info.Traits.Get<BridgeInfo>().Long ? 6 : 3;
			for (var n = 0; n < numStates; n++)
			{
				var stateTemplate = world.TileSet.Walkability.GetTileTemplate(NameFromState(template, n));
				Templates.Add( stateTemplate );

				TileSprites.Add(replacedTiles.ToDictionary(
					a => a.Key,
					a => sprites[new TileReference<ushort,byte>((ushort)stateTemplate.Index, (byte)a.Value)]));
			}

			self.Health = (int)(self.GetMaxHP() * template.HP);
		}

		public float GetCost(int2 p, UnitMovementType umt)
		{
			return Rules.TerrainTypes[Templates[state].TerrainType[Tiles[p]]].GetCost(umt);
		}
		
		public float GetSpeedModifier(int2 p, UnitMovementType umt)
		{
			return Rules.TerrainTypes[Templates[state].TerrainType[Tiles[p]]].GetSpeedModifier(umt);
		}
		
		static bool IsIntact(Bridge b)
		{
			return b != null && b.self.IsInWorld && b.self.Health > 0;
		}

		static bool IsLong(Bridge b)
		{
			return b != null && b.self.IsInWorld && b.self.Info.Traits.Get<BridgeInfo>().Long;
		}

		void UpdateState()
		{
			var ds = self.GetDamageState();
			if (!self.Info.Traits.Get<BridgeInfo>().Long)
			{
				state = (int)ds; 
				return;
			}

			bool waterToSouth = !IsIntact(southNeighbour) && (!IsLong(southNeighbour) || !IsIntact(this));
			bool waterToNorth = !IsIntact(northNeighbour) && (!IsLong(northNeighbour) || !IsIntact(this));

			if (waterToSouth && waterToNorth) { state = 5; return; }
			if (waterToNorth) { state = 4; return; }
			if (waterToSouth) { state = 3; return; }
			state = (int)ds;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageStateChanged)
			{
				UpdateState();
				if (northNeighbour != null) northNeighbour.UpdateState();
				if (southNeighbour != null) southNeighbour.UpdateState();
			}
		}
	}
}
