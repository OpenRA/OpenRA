#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	[Desc("This actor triggers an explosion on itself when transitioning to a specific damage state.")]
	public class ExplosionOnDamageTransitionInfo : ITraitInfo, IRulesetLoaded, Requires<HealthInfo>
	{
		[WeaponReference, FieldLoader.Require, Desc("Weapon to use for explosion.")]
		public readonly string Weapon = null;

		[Desc("At which damage state explosion will trigger.")]
		public readonly DamageState DamageState = DamageState.Heavy;

		[Desc("Should the explosion only be triggered once?")]
		public readonly bool TriggerOnlyOnce = false;

		public WeaponInfo WeaponInfo { get; private set; }

		public object Create(ActorInitializer init) { return new ExplosionOnDamageTransition(this, init.Self); }

		public void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (string.IsNullOrEmpty(Weapon))
				return;

			WeaponInfo weapon;
			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out weapon))
				throw new YamlException("Weapons Ruleset does not contain an entry '{0}'".F(weaponToLower));

			WeaponInfo = weapon;
		}
	}

	public class ExplosionOnDamageTransition : INotifyDamageStateChanged
	{
		readonly ExplosionOnDamageTransitionInfo info;
		bool triggered;

		public ExplosionOnDamageTransition(ExplosionOnDamageTransitionInfo info, Actor self)
		{
			this.info = info;
		}

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (!self.IsInWorld)
				return;

			if (triggered)
				return;

			if (e.DamageState >= info.DamageState && e.PreviousDamageState < info.DamageState)
			{
				if (info.TriggerOnlyOnce)
					triggered = true;

				// Use .FromPos since the actor might have been killed, don't use Target.FromActor
				info.WeaponInfo.Impact(Target.FromPos(self.CenterPosition), e.Attacker, Enumerable.Empty<int>());
			}
		}
	}
}
