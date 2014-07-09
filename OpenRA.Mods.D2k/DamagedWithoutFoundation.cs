#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	[Desc("Reduces health points over time when the actor is placed on unsafe terrain.")]
	class DamagedWithoutFoundationInfo : ITraitInfo, Requires<HealthInfo>
	{
		[WeaponReference]
		public readonly string Weapon = "weathering";
		public readonly string[] SafeTerrain = { "Concrete" };
		public readonly int DamageThreshold = 50;

		public object Create(ActorInitializer init) { return new DamagedWithoutFoundation(init.self, this); }
	}

	class DamagedWithoutFoundation : ITick, ISync, INotifyAddedToWorld
	{
		readonly DamagedWithoutFoundationInfo info;
		readonly Health health;
		readonly WeaponInfo weapon;

		[Sync] int damageThreshold = 100;
		[Sync] int damageTicks;

		public DamagedWithoutFoundation(Actor self, DamagedWithoutFoundationInfo info)
		{
			this.info = info;
			health = self.Trait<Health>();
			weapon = self.World.Map.Rules.Weapons[info.Weapon.ToLowerInvariant()];
		}

		public void AddedToWorld(Actor self)
		{
			var safeTiles = 0;
			var totalTiles = 0;
			foreach (var kv in self.OccupiesSpace.OccupiedCells())
			{
				totalTiles++;
				if (info.SafeTerrain.Contains(self.World.Map.GetTerrainInfo(kv.First).Type))
					safeTiles++;
			}

			damageThreshold = (info.DamageThreshold * health.MaxHP + (100 - info.DamageThreshold) * safeTiles * health.MaxHP / totalTiles) / 100;

			// Actors start with maximum damage applied
			var delta = health.HP - damageThreshold;
			if (delta > 0)
				health.InflictDamage(self, self.World.WorldActor, delta, null, false);
		}

		public void Tick(Actor self)
		{
			if (health.HP <= damageThreshold || --damageTicks > 0)
				return;

			foreach (var w in weapon.Warheads)
				if (w is DamagerWarheadInfo)
					health.InflictDamage(self, self.World.WorldActor, (w as DamagerWarheadInfo).Damage, (w as DamagerWarheadInfo), false);

			damageTicks = weapon.ROF;
		}
	}
}
