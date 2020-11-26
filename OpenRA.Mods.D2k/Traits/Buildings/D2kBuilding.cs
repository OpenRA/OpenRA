#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits.Buildings
{
	public class D2kBuildingInfo : BuildingInfo
	{
		[Desc("Amount of damage received per DamageInterval ticks.")]
		public readonly int Damage = 500;

		[Desc("Delay between receiving damage.")]
		public readonly int DamageInterval = 100;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly BitSet<DamageType> DamageTypes = default(BitSet<DamageType>);

		[Desc("Terrain types where the actor will take damage.")]
		public readonly string[] DamageTerrainTypes = { "Rock" };

		[Desc("Percentage health below which the actor will not receive further damage.")]
		public readonly int DamageThreshold = 50;

		[Desc("Inflict damage down to the DamageThreshold when the actor gets created on damaging terrain.")]
		public readonly bool StartOnThreshold = true;

		[Desc("The terrain template to place when adding a concrete foundation. " +
			"If the template is PickAny, then the actor footprint will be filled with this tile.")]
		public readonly ushort ConcreteTemplate = 88;

		[Desc("List of required prerequisites to place a terrain template.")]
		public readonly string[] ConcretePrerequisites = { };

		public override object Create(ActorInitializer init) { return new D2kBuilding(init, this); }
	}

	public class D2kBuilding : Building, ITick, INotifyCreated
	{
		readonly D2kBuildingInfo info;

		BuildableTerrainLayer layer;
		IHealth health;
		int safeTiles;
		int totalTiles;
		int damageThreshold;
		int damageTicks;
		TechTree techTree;
		BuildingInfluence bi;

		public D2kBuilding(ActorInitializer init, D2kBuildingInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			health = self.TraitOrDefault<IHealth>();
			layer = self.World.WorldActor.TraitOrDefault<BuildableTerrainLayer>();
			bi = self.World.WorldActor.Trait<BuildingInfluence>();
			techTree = self.Owner.PlayerActor.TraitOrDefault<TechTree>();
		}

		protected override void AddedToWorld(Actor self)
		{
			base.AddedToWorld(self);

			if (layer != null && (!info.ConcretePrerequisites.Any() || techTree == null || techTree.HasPrerequisites(info.ConcretePrerequisites)))
			{
				var map = self.World.Map;
				var template = map.Rules.TileSet.Templates[info.ConcreteTemplate];
				if (template.PickAny)
				{
					// Fill the footprint with random variants
					foreach (var c in info.Tiles(self.Location))
					{
						if (!map.Contains(c) || map.CustomTerrain[c] != byte.MaxValue)
							continue;

						// Don't place under other buildings (or their bib)
						if (bi.GetBuildingAt(c) != self)
							continue;

						var index = Game.CosmeticRandom.Next(template.TilesCount);
						layer.AddTile(c, new TerrainTile(template.Id, (byte)index));
					}
				}
				else
				{
					for (var i = 0; i < template.TilesCount; i++)
					{
						var c = self.Location + new CVec(i % template.Size.X, i / template.Size.X);
						if (!map.Contains(c) || map.CustomTerrain[c] != byte.MaxValue)
							continue;

						// Don't place under other buildings (or their bib)
						if (bi.GetBuildingAt(c) != self)
							continue;

						layer.AddTile(c, new TerrainTile(template.Id, (byte)i));
					}
				}
			}

			if (health == null)
				return;

			foreach (var kv in self.OccupiesSpace.OccupiedCells())
			{
				totalTiles++;
				if (!info.DamageTerrainTypes.Contains(self.World.Map.GetTerrainInfo(kv.Cell).Type))
					safeTiles++;
			}

			if (totalTiles == 0 || totalTiles == safeTiles)
				return;

			// Cast to long to avoid overflow when multiplying by the health
			damageThreshold = (int)((info.DamageThreshold * (long)health.MaxHP + (100 - info.DamageThreshold) * safeTiles * (long)health.MaxHP / totalTiles) / 100);

			if (!info.StartOnThreshold)
				return;

			// Start with maximum damage applied
			var delta = health.HP - damageThreshold;
			if (delta > 0)
				self.InflictDamage(self.World.WorldActor, new Damage(delta, info.DamageTypes));
		}

		void ITick.Tick(Actor self)
		{
			if (totalTiles == safeTiles || health.HP <= damageThreshold || --damageTicks > 0)
				return;

			self.InflictDamage(self.World.WorldActor, new Damage(info.Damage, info.DamageTypes));
			damageTicks = info.DamageInterval;
		}
	}
}
