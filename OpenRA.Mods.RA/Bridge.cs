#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BridgeInfo : ITraitInfo, ITraitPrerequisite<HealthInfo>
	{
		public readonly bool Long = false;
		
		#pragma warning disable 0649
		public readonly ushort Template;
		public readonly ushort DamagedTemplate;
		public readonly ushort DestroyedTemplate;
		
		// For long bridges
		public readonly ushort DestroyedPlusNorthTemplate;
		public readonly ushort DestroyedPlusSouthTemplate;
		public readonly ushort DestroyedPlusBothTemplate;
		#pragma warning restore 0649
		public readonly string[] ShorePieces = {"br1", "br2"};
		public readonly int[] NorthOffset = null;
		public readonly int[] SouthOffset = null;

		public object Create(ActorInitializer init) { return new Bridge(init.self, this); }

		public IEnumerable<Pair<ushort, float>> Templates
		{
			get
			{
				if (Template != 0)
					yield return Pair.New(Template, 1f);

				if (DamagedTemplate != 0)
					yield return Pair.New(DamagedTemplate, .5f);

				if (DestroyedTemplate != 0)
					yield return Pair.New(DestroyedTemplate, 0f);

				if (DestroyedPlusNorthTemplate != 0)
					yield return Pair.New(DestroyedPlusNorthTemplate, 0f);

				if (DestroyedPlusSouthTemplate != 0)
					yield return Pair.New(DestroyedPlusSouthTemplate, 0f);

				if (DestroyedPlusBothTemplate != 0)
					yield return Pair.New(DestroyedPlusBothTemplate, 0f);
			}
		}
	}

	class Bridge: IRender, INotifyDamage
	{
		static string cachedTileset;
		static Cache<TileReference<ushort,byte>, Sprite> sprites;

		Dictionary<ushort, Dictionary<int2, Sprite>> TileSprites = new Dictionary<ushort, Dictionary<int2, Sprite>>();
		Dictionary<ushort, TileTemplate> Templates = new Dictionary<ushort, TileTemplate>();
		ushort currentTemplate;
		
		Actor self;
		BridgeInfo Info;
		public string Type;
		Bridge northNeighbour, southNeighbour;
		Health Health;
		
		public Bridge(Actor self, BridgeInfo info)
		{
			this.self = self;
			Health = self.Trait<Health>();
			Health.RemoveOnDeath = false;
			this.Info = info;
			this.Type = self.Info.Name;
		}

		public void Create(ushort template, Dictionary<int2, byte> subtiles)
		{
			currentTemplate = template;
				
			// Create a new cache to store the tile data
			if (cachedTileset != self.World.Map.Tileset)
			{
				cachedTileset = self.World.Map.Tileset;
				sprites = new Cache<TileReference<ushort,byte>, Sprite>(
				x => Game.modData.SheetBuilder.Add(self.World.TileSet.GetBytes(x),
					new Size(Game.CellSize, Game.CellSize)));
			}
			
			// Cache templates and tiles for the different states
			foreach (var t in Info.Templates)
			{
				Templates.Add(t.First,self.World.TileSet.Templates[t.First]);
				TileSprites.Add(t.First,subtiles.ToDictionary(
					a => a.Key,
					a => sprites[new TileReference<ushort,byte>(t.First, (byte)a.Value)]));
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
			if (Info.NorthOffset != null)
				northNeighbour = GetNeighbor(Info.NorthOffset, bridges);
			if (Info.SouthOffset != null)
				southNeighbour = GetNeighbor(Info.SouthOffset, bridges);
		}
		
		public Bridge GetNeighbor(int[] offset, BridgeLayer bridges)
		{
			if (offset == null) return null;
			return bridges.GetBridge(self.Location + new int2(offset[0], offset[1]));
		}

		public IEnumerable<Renderable> Render(Actor self)
		{
			foreach (var t in TileSprites[currentTemplate])
				yield return new Renderable(t.Value, Game.CellSize * t.Key, "terrain", Game.CellSize * t.Key.Y);
		}
		
		bool IsIntact(Bridge b)
		{
			return b != null && b.self.IsInWorld && !b.self.IsDead();
		}

		void KillUnitsOnBridge()
		{
			var uim = self.World.WorldActor.Trait<UnitInfluence>();

			foreach (var c in TileSprites[currentTemplate].Keys)
				foreach (var a in uim.GetUnitsAt(c))
					if (a.HasTrait<IMove>() && !a.Trait<IMove>().CanEnterCell(c))
						a.Kill(self);
		}
		
		bool dead = false;
		void UpdateState()
		{						
			// If this is a long bridge next to a destroyed shore piece, we need die to give clean edges to the break
			if (Info.Long && Health.DamageState != DamageState.Dead &&
			    ((southNeighbour != null && Info.ShorePieces.Contains(southNeighbour.Type) && !IsIntact(southNeighbour)) ||
				(northNeighbour != null && Info.ShorePieces.Contains(northNeighbour.Type) && !IsIntact(northNeighbour))))
			{
				self.Kill(self); // this changes the damagestate
			}

			var ds = Health.DamageState;
			currentTemplate = (ds == DamageState.Dead && Info.DestroyedTemplate > 0) ? Info.DestroyedTemplate :
							  (ds >= DamageState.Heavy && Info.DamagedTemplate > 0) ? Info.DamagedTemplate : Info.Template;

			if (Info.Long && ds == DamageState.Dead)
			{
				// Long bridges have custom art for multiple segments being destroyed
				bool waterToSouth = !IsIntact(southNeighbour);
				bool waterToNorth = !IsIntact(northNeighbour);

				if (waterToSouth && waterToNorth)
					currentTemplate = Info.DestroyedPlusBothTemplate;
				else if (waterToNorth)
					currentTemplate = Info.DestroyedPlusNorthTemplate;
				else if (waterToSouth)
					currentTemplate = Info.DestroyedPlusSouthTemplate;
			}

			if (ds == DamageState.Dead && !dead)
			{
				dead = true;
				KillUnitsOnBridge();
			}

			// Update map
			foreach (var c in TileSprites[currentTemplate].Keys)
				self.World.Map.CustomTerrain[c.X, c.Y] = GetTerrainType(c);
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
