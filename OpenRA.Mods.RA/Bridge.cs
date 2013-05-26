#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BridgeInfo : ITraitInfo, Requires<HealthInfo>
	{
		public readonly bool Long = false;

		[Desc("Delay (in ticks) between repairing adjacent spans in long bridges")]
		public readonly int RepairPropagationDelay = 20;

		public readonly ushort Template = 0;
		public readonly ushort DamagedTemplate = 0;
		public readonly ushort DestroyedTemplate = 0;

		// For long bridges
		public readonly ushort DestroyedPlusNorthTemplate = 0;
		public readonly ushort DestroyedPlusSouthTemplate = 0;
		public readonly ushort DestroyedPlusBothTemplate = 0;

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

	class Bridge: IRenderAsTerrain, INotifyDamageStateChanged
	{
		static string cachedTileset;
		static Cache<TileReference<ushort,byte>, Sprite> sprites;

		Dictionary<ushort, Dictionary<CPos, Sprite>> TileSprites = new Dictionary<ushort, Dictionary<CPos, Sprite>>();
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

		public void Create(ushort template, Dictionary<CPos, byte> subtiles)
		{
			currentTemplate = template;

			// Create a new cache to store the tile data
			if (cachedTileset != self.World.Map.Tileset)
			{
				cachedTileset = self.World.Map.Tileset;
				var tileSize = new Size(Game.CellSize, Game.CellSize);
				sprites = new Cache<TileReference<ushort,byte>, Sprite>(
					x => Game.modData.SheetBuilder.Add(self.World.TileSet.GetBytes(x), tileSize, true));
			}

			// Cache templates and tiles for the different states
			foreach (var t in Info.Templates)
			{
				Templates.Add(t.First,self.World.TileSet.Templates[t.First]);
				TileSprites.Add(t.First, subtiles.ToDictionary(
					a => a.Key,
					a => sprites[new TileReference<ushort,byte>(t.First, (byte)a.Value)]));
			}

			// Set the initial custom terrain types
			foreach (var c in TileSprites[currentTemplate].Keys)
				self.World.Map.CustomTerrain[c.X, c.Y] = GetTerrainType(c);
		}

		public string GetTerrainType(CPos cell)
		{
			var dx = cell - self.Location;
			var index = dx.X + Templates[currentTemplate].Size.X * dx.Y;
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
			return bridges.GetBridge(self.Location + new CVec(offset[0], offset[1]));
		}

		bool initializePalettes = true;
		PaletteReference terrainPalette;
		public IEnumerable<IRenderable> RenderAsTerrain(WorldRenderer wr, Actor self)
		{
			if (initializePalettes)
			{
				terrainPalette = wr.Palette("terrain");
				initializePalettes = false;
			}

			foreach (var t in TileSprites[currentTemplate])
				yield return new SpriteRenderable(t.Value, t.Key.CenterPosition, 0, terrainPalette, 1f);
		}

		void KillUnitsOnBridge()
		{
			foreach (var c in TileSprites[currentTemplate].Keys)
				foreach (var a in self.World.ActorMap.GetUnitsAt(c))
					if (a.HasTrait<IMove>() && !a.Trait<IMove>().CanEnterCell(c))
						a.Kill(self);
		}

		bool NeighbourIsDeadShore(Bridge neighbour)
		{
			return neighbour != null && Info.ShorePieces.Contains(neighbour.Type) && neighbour.Health.IsDead;
		}

		bool LongBridgeSegmentIsDead()
		{
			// The long bridge artwork requires a hack to display correctly
			// if the adjacent shore piece is dead
			if (!Info.Long)
				return Health.IsDead;

			if (NeighbourIsDeadShore(northNeighbour))
				return true;

			if (NeighbourIsDeadShore(southNeighbour))
				return true;

			return Health.IsDead;
		}

		ushort ChooseTemplate()
		{
			if (Info.Long && LongBridgeSegmentIsDead())
			{
				// Long bridges have custom art for multiple segments being destroyed
				var northIsDead = northNeighbour != null && northNeighbour.LongBridgeSegmentIsDead();
				var southIsDead = southNeighbour != null && southNeighbour.LongBridgeSegmentIsDead();
				if (northIsDead && southIsDead)
					return Info.DestroyedPlusBothTemplate;
				if (northIsDead)
					return Info.DestroyedPlusNorthTemplate;
				if (southIsDead)
					return Info.DestroyedPlusSouthTemplate;

				return Info.DestroyedTemplate;
			}

			var ds = Health.DamageState;
			return (ds == DamageState.Dead && Info.DestroyedTemplate > 0) ? Info.DestroyedTemplate :
				   (ds >= DamageState.Heavy && Info.DamagedTemplate > 0) ? Info.DamagedTemplate : Info.Template;
		}

		bool killedUnits = false;
		void UpdateState()
		{
			var oldTemplate = currentTemplate;

			currentTemplate = ChooseTemplate();
			if (currentTemplate == oldTemplate)
				return;

			// Update map
			foreach (var c in TileSprites[currentTemplate].Keys)
				self.World.Map.CustomTerrain[c.X, c.Y] = GetTerrainType(c);

			if (LongBridgeSegmentIsDead() && !killedUnits)
			{
				killedUnits = true;
				KillUnitsOnBridge();
			}
		}

		public void Repair(Actor repairer, bool continueNorth, bool continueSouth)
		{
			// Repair self
			var initialDamage = Health.DamageState;
			self.World.AddFrameEndTask(w =>
			{
				if (Health.IsDead)
				{
					Health.Resurrect(self, repairer);
					killedUnits = false;
					KillUnitsOnBridge();
				}
				else
					Health.InflictDamage(self, repairer, -Health.MaxHP, null, true);
			});

			// Repair adjacent spans (long bridges)
			if (continueNorth && northNeighbour != null)
			{
				var delay = initialDamage == DamageState.Undamaged || NeighbourIsDeadShore(northNeighbour) ?
					0 : Info.RepairPropagationDelay;

				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(delay, () =>
					northNeighbour.Repair(repairer, true, false))));
			}

			if (continueSouth && southNeighbour != null)
			{
				var delay = initialDamage == DamageState.Undamaged || NeighbourIsDeadShore(southNeighbour) ?
					0 : Info.RepairPropagationDelay;

				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(delay, () =>
					southNeighbour.Repair(repairer, false, true))));
			}
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			UpdateState();
			if (northNeighbour != null)
				northNeighbour.UpdateState();
			if (southNeighbour != null)
				southNeighbour.UpdateState();

			// Need to update the neighbours neighbour to correctly
			// display the broken shore hack
			if (Info.ShorePieces.Contains(Type))
			{
				if (northNeighbour != null && northNeighbour.northNeighbour != null)
					northNeighbour.northNeighbour.UpdateState();
				if (southNeighbour != null && southNeighbour.southNeighbour != null)
					southNeighbour.southNeighbour.UpdateState();
			}
		}

		public DamageState AggregateDamageState()
		{
			// Find the worst span damage in the entire bridge
			var br = this;
			while (br.northNeighbour != null)
				br = br.northNeighbour;

			var damage = Health.DamageState;
			for (var b = br; b != null; b = b.southNeighbour)
				if (b.Health.DamageState > damage)
					damage = b.Health.DamageState;

			return damage;
		}
	}
}
