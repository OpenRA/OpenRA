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
using OpenRA.Traits;
using OpenRA.GameRules;
using OpenRA.Mods.Common;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor is attacked by the given weapon when he is in the specified terrain type.")]
	class DamagedByTerrainInfo : UpgradableTraitInfo, ITraitInfo, Requires<HealthInfo>
	{
		[Desc("The weapon which is used on the actor.")]
		[WeaponReference] public readonly string Weapon;

		[Desc("Terrain types where the actor will be attacked.")]
		public readonly string[] Terrain;

		[Desc("Minimum health on the actor. In percent.")]
		public readonly int DamageThreshold = 0;

		[Desc("Start the health on the minimum amount if he is produced on the specified terrain type.",
			"This is scaled by the footprint so an actor that takes a 2*2 grid and only 2 of those are bad,",
			"then he will only take half the damage he would have if all the tiles were bad.")]
		public readonly bool StartOnThreshold = false;

		public object Create(ActorInitializer init) { return new DamagedByTerrain(this, init.self); }
	}

	class DamagedByTerrain : UpgradableTrait<DamagedByTerrainInfo>, ITick, ISync, INotifyAddedToWorld
	{
		readonly Health health;
		readonly WeaponInfo weapon;

		[Sync] int damageTicks;
		[Sync] int damageThreshold;

		public DamagedByTerrain(DamagedByTerrainInfo info, Actor self) : base(info)
		{
			weapon = self.World.Map.Rules.Weapons[info.Weapon.ToLowerInvariant()];
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

			damageThreshold = (Info.DamageThreshold * health.MaxHP + (100 - Info.DamageThreshold) * safeTiles * health.MaxHP / totalTiles) / 100;

			// Actors start with maximum damage applied
			var delta = health.HP - damageThreshold;
			if (delta > 0)
				health.InflictDamage(self, self.World.WorldActor, delta, null, false);
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

			weapon.Impact(Target.FromActor(self), self.World.WorldActor, Enumerable.Empty<int>());
			damageTicks = weapon.ReloadDelay;
		}
	}
}
