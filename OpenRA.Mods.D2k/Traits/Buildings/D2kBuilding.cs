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

		public override object Create(ActorInitializer init) { return new D2kBuilding(init, this); }
	}

	public class D2kBuilding : Building, ITick, INotifyCreated
	{
		readonly D2kBuildingInfo info;
		IHealth health;
		int safeTiles;
		int totalTiles;
		int damageThreshold;
		int damageTicks;

		public D2kBuilding(ActorInitializer init, D2kBuildingInfo info)
			: base(init, info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			health = self.TraitOrDefault<IHealth>();
		}

		protected override void AddedToWorld(Actor self)
		{
			base.AddedToWorld(self);

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
