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
using System;
using OpenRA.FileFormats;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BridgeInfo : ITraitInfo
	{
		public readonly bool Long = false;
		
		public readonly ushort Template;
		public readonly float DamagedThreshold = 0.5f;
		public readonly ushort DamagedTemplate;
		public readonly ushort DestroyedTemplate;
		
		// For long bridges
		public readonly ushort DestroyedPlusNorthTemplate;
		public readonly ushort DestroyedPlusSouthTemplate;
		public readonly ushort DestroyedPlusBothTemplate;

		public readonly bool UseAlternateNames = false;
		public readonly int[] NorthOffset = null;
		public readonly int[] SouthOffset = null;

		public object Create(ActorInitializer init) { return new Bridge(init.self, this); }
		
		public IEnumerable<ushort> Templates
		{ get {
		
			if (Template != 0)
				yield return Template;
			
			if (DamagedTemplate != 0)
				yield return DamagedTemplate;
			
			if (DestroyedTemplate != 0)
				yield return DestroyedTemplate;
						
			if (DestroyedPlusNorthTemplate != 0)
				yield return DestroyedPlusNorthTemplate;
			
			if (DestroyedPlusSouthTemplate != 0)
				yield return DestroyedPlusSouthTemplate;
			
			if (DestroyedPlusBothTemplate != 0)
				yield return DestroyedPlusBothTemplate;
		} }
	}

	class Bridge: IRender, INotifyDamage
	{
		static string cachedTileset;
		static Cache<TileReference<ushort,byte>, Sprite> sprites;

		Dictionary<ushort, Dictionary<int2, Sprite>> TileSprites = new Dictionary<ushort, Dictionary<int2, Sprite>>();
		Dictionary<ushort, TileTemplate> Templates = new Dictionary<ushort, TileTemplate>();
		ushort currentTemplate;
		
		Actor self;
		BridgeInfo info;
		Bridge northNeighbour, southNeighbour;
		
		public Bridge(Actor self, BridgeInfo info)
		{
			this.self = self;
			self.RemoveOnDeath = false;
			this.info = info;
		}

		public void Create(ushort template, Dictionary<int2, byte> subtiles)
		{
			currentTemplate = template;
			if (template == info.DamagedTemplate)
				self.Health = (int)(info.DamagedThreshold*self.GetMaxHP());
			else if (template != info.Template)
				self.Health = 0;
						
			// Create a new cache to store the tile data
			if (cachedTileset != self.World.Map.Tileset)
			{
				cachedTileset = self.World.Map.Tileset;
				sprites = new Cache<TileReference<ushort,byte>, Sprite>(
				x => SheetBuilder.SharedInstance.Add(self.World.TileSet.GetBytes(x),
					new Size(Game.CellSize, Game.CellSize)));
			}
			
			// Cache templates and tiles for the different states
			foreach (var t in info.Templates)
			{
				Templates.Add(t,self.World.TileSet.Templates[t]);
				TileSprites.Add(t,subtiles.ToDictionary(
					a => a.Key,
					a => sprites[new TileReference<ushort,byte>(t, (byte)a.Value)]));
			}
		}
		
		public string GetTerrainType(int2 cell)
		{
			var dx = cell - self.Location;
			var index = dx.X + Templates[currentTemplate].Size.X*dx.Y;
			return self.World.TileSet.GetTerrainType(new TileReference<ushort, byte>(currentTemplate,(byte)index));
		}
		
		public void LinkNeighbouringBridges(World world, BridgeLayer bridges)
		{
			// go looking for our neighbors if this is a long bridge.
			var info = self.Info.Traits.Get<BridgeInfo>();
			if (info.NorthOffset != null)
				northNeighbour = GetNeighbor(info.NorthOffset, bridges);
			if (info.SouthOffset != null)
				southNeighbour = GetNeighbor(info.SouthOffset, bridges);
		}
		
		public Bridge GetNeighbor(int[] offset, BridgeLayer bridges)
		{
			if (offset == null) return null;
			return bridges.GetBridge(self.Location + new int2(offset[0], offset[1]));
		}

		public IEnumerable<Renderable> Render(Actor self)
		{
			foreach (var t in TileSprites[currentTemplate])
				yield return new Renderable(t.Value, Game.CellSize * t.Key, "terrain");
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
			/*var ds = self.GetDamageState();
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
			*/
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			/*if (e.DamageStateChanged)
			{
				UpdateState();
				if (northNeighbour != null) northNeighbour.UpdateState();
				if (southNeighbour != null) southNeighbour.UpdateState();
			}*/
		}
	}
}
