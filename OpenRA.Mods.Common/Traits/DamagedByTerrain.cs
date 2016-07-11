#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor receives damage from the given weapon when on the specified terrain type.")]
	class DamagedByTerrainInfo : UpgradableTraitInfo, Requires<HealthInfo>
	{
		[Desc("Amount of damage received per DamageInterval ticks.")]
		[FieldLoader.Require] public readonly int Damage = 0;

		[Desc("Delay between receiving damage.")]
		public readonly int DamageInterval = 0;

		[Desc("Apply the damage using these damagetypes.")]
		public readonly HashSet<string> DamageTypes = new HashSet<string>();

		[Desc("Terrain types where the actor will take damage.")]
		[FieldLoader.Require] public readonly string[] Terrain = { };

		[Desc("Percentage health below which the actor will not receive further damage.")]
		public readonly int DamageThreshold = 0;

		[Desc("Inflict damage down to the DamageThreshold when the actor gets created on damaging terrain.")]
		public readonly bool StartOnThreshold = false;

		public override object Create(ActorInitializer init) { return new DamagedByTerrain(init.Self, this); }
	}

	class DamagedByTerrain : UpgradableTrait<DamagedByTerrainInfo>, ITick, ISync, INotifyAddedToWorld
	{
		readonly Health health;

		[Sync] int damageTicks;
		[Sync] int damageThreshold;

		public DamagedByTerrain(Actor self, DamagedByTerrainInfo info) : base(info)
		{
			health = self.Trait<Health>();
		}

		public void AddedToWorld(Actor self)
		{
			if (!Info.StartOnThreshold)
				return;

			var safeTiles = 0;
			var totalTiles = 0;
			foreach (var kv in self.OccupiesSpace.OccupiedCells())
			{
				totalTiles++;
				if (!Info.Terrain.Contains(self.World.Map.GetTerrainInfo(kv.First).Type))
					safeTiles++;
			}

			if (totalTiles == 0)
				return;

			damageThreshold = (Info.DamageThreshold * health.MaxHP + (100 - Info.DamageThreshold) * safeTiles * health.MaxHP / totalTiles) / 100;

			// Actors start with maximum damage applied
			var delta = health.HP - damageThreshold;
			if (delta > 0)
				self.InflictDamage(self.World.WorldActor, new Damage(delta, Info.DamageTypes));
		}

		public void Tick(Actor self)
		{
			if (IsTraitDisabled || health.HP <= damageThreshold || --damageTicks > 0)
				return;

			// Prevents harming cargo.
			if (!self.IsInWorld)
				return;

			var t = self.World.Map.GetTerrainInfo(self.Location);
			if (!Info.Terrain.Contains(t.Type))
				return;

			self.InflictDamage(self.World.WorldActor, new Damage(Info.Damage, Info.DamageTypes));
			damageTicks = Info.DamageInterval;
		}
	}
}
