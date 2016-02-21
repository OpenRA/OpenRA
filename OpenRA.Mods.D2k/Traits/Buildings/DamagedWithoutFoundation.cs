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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Reduces health points over time when the actor is placed on unsafe terrain.")]
	class DamagedWithoutFoundationInfo : ITraitInfo, IRulesetLoaded, Requires<HealthInfo>
	{
		[WeaponReference, Desc("The weapon to use for causing damage.")]
		public readonly string Weapon = "weathering";

		[Desc("Terrain types on which no damage is caused.")]
		public readonly HashSet<string> SafeTerrain = new HashSet<string> { "Concrete" };

		[Desc("The percentage of health the actor should keep.")]
		public readonly int DamageThreshold = 50;

		public WeaponInfo WeaponInfo { get; private set; }

		public object Create(ActorInitializer init) { return new DamagedWithoutFoundation(init.Self, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai) { WeaponInfo = rules.Weapons[Weapon.ToLowerInvariant()]; }
	}

	class DamagedWithoutFoundation : ITick, ISync, INotifyAddedToWorld
	{
		readonly DamagedWithoutFoundationInfo info;
		readonly Health health;

		[Sync] int damageThreshold = 100;
		[Sync] int damageTicks;

		public DamagedWithoutFoundation(Actor self, DamagedWithoutFoundationInfo info)
		{
			this.info = info;
			health = self.Trait<Health>();
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

			if (totalTiles > 0)
				damageThreshold = (info.DamageThreshold * health.MaxHP + (100 - info.DamageThreshold) * safeTiles * health.MaxHP / totalTiles) / 100;
			else
				damageThreshold = health.HP;

			// Actors start with maximum damage applied
			var delta = health.HP - damageThreshold;
			if (delta > 0)
				health.InflictDamage(self, self.World.WorldActor, delta, null, false);

			damageTicks = info.WeaponInfo.ReloadDelay;
		}

		public void Tick(Actor self)
		{
			if (health.HP <= damageThreshold || --damageTicks > 0)
				return;

			info.WeaponInfo.Impact(Target.FromActor(self), self.World.WorldActor, Enumerable.Empty<int>());
			damageTicks = info.WeaponInfo.ReloadDelay;
		}
	}
}
