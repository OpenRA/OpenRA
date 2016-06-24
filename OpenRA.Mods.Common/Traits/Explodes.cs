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
using OpenRA.Mods.Common.Warheads;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor explodes when killed.")]
	public class ExplodesInfo : ITraitInfo, IRulesetLoaded, Requires<HealthInfo>
	{
		[WeaponReference, FieldLoader.Require, Desc("Weapon to use for explosion if ammo/payload is loaded.")]
		public readonly string Weapon = "UnitExplode";

		[WeaponReference, Desc("Weapon to use for explosion if no ammo/payload is loaded.")]
		public readonly string EmptyWeapon = "UnitExplode";

		[Desc("Chance that the explosion will use Weapon if the actor has ammo/payload.")]
		public readonly int LoadedChance = 100;

		[Desc("Chance that this actor will explode at all.")]
		public readonly int Chance = 100;

		[Desc("Health level at which actor will explode.")]
		public readonly int DamageThreshold = 0;

		[Desc("DeathType(s) that trigger the explosion. Leave empty to always trigger an explosion.")]
		public readonly HashSet<string> DeathTypes = new HashSet<string>();

		public WeaponInfo WeaponInfo { get; private set; }
		public WeaponInfo EmptyWeaponInfo { get; private set; }

		public object Create(ActorInitializer init) { return new Explodes(this, init.Self); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			WeaponInfo = string.IsNullOrEmpty(Weapon) ? null : rules.Weapons[Weapon.ToLowerInvariant()];
			EmptyWeaponInfo = string.IsNullOrEmpty(EmptyWeapon) ? null : rules.Weapons[EmptyWeapon.ToLowerInvariant()];
		}
	}

	public class Explodes : INotifyKilled, INotifyDamage
	{
		readonly ExplodesInfo info;

		readonly Health health;

		public Explodes(ExplodesInfo info, Actor self)
		{
			this.info = info;
			health = self.Trait<Health>();
		}

		public void Killed(Actor self, AttackInfo e)
		{
			if (!self.IsInWorld)
				return;

			if (self.World.SharedRandom.Next(100) > info.Chance)
				return;

			if (info.DeathTypes.Count > 0 && !e.Damage.DamageTypes.Overlaps(info.DeathTypes))
				return;

			var weapon = ChooseWeaponForExplosion(self);
			if (weapon == null)
				return;

			if (weapon.Report != null && weapon.Report.Any())
				Game.Sound.Play(weapon.Report.Random(e.Attacker.World.SharedRandom), self.CenterPosition);

			// Use .FromPos since this actor is killed. Cannot use Target.FromActor
			weapon.Impact(Target.FromPos(self.CenterPosition), e.Attacker, Enumerable.Empty<int>());
		}

		WeaponInfo ChooseWeaponForExplosion(Actor self)
		{
			var shouldExplode = self.TraitsImplementing<IExplodeModifier>().All(a => a.ShouldExplode(self));
			var useFullExplosion = self.World.SharedRandom.Next(100) <= info.LoadedChance;
			return (shouldExplode && useFullExplosion) ? info.WeaponInfo : info.EmptyWeaponInfo;
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (info.DamageThreshold == 0)
				return;

			if (health.HP * 100 < info.DamageThreshold * health.MaxHP)
				self.World.AddFrameEndTask(w => self.Kill(e.Attacker));
		}
	}
}
