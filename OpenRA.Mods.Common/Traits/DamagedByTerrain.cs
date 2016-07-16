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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor receives damage from the given weapon when on the specified terrain type.")]
	class DamagedByTerrainInfo : UpgradableTraitInfo, IRulesetLoaded, Requires<HealthInfo>
	{
		[Desc("The weapon which is used to damage the actor.")]
		[WeaponReference, FieldLoader.Require] public readonly string Weapon;

		[Desc("Terrain types where the actor will take damage.")]
		[FieldLoader.Require] public readonly string[] Terrain = { };

		[Desc("Percentage health below which the actor will not receive further damage.")]
		public readonly int DamageThreshold = 0;

		[Desc("Inflict damage down to the DamageThreshold when the actor gets created on damaging terrain.")]
		public readonly bool StartOnThreshold = false;

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new DamagedByTerrain(init.Self, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai) { WeaponInfo = rules.Weapons[Weapon.ToLowerInvariant()]; }
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
				health.InflictDamage(self, self.World.WorldActor, new Damage(delta), false);
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

			Info.WeaponInfo.Impact(Target.FromActor(self), self.World.WorldActor, Enumerable.Empty<int>());
			damageTicks = Info.WeaponInfo.ReloadDelay;
		}
	}
}
