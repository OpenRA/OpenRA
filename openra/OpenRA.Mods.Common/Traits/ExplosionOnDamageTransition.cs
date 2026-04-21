#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor triggers an explosion on itself when transitioning to a specific damage state.")]
	public class ExplosionOnDamageTransitionInfo : ConditionalTraitInfo, IRulesetLoaded, Requires<IHealthInfo>
	{
		[WeaponReference]
		[FieldLoader.Require]
		[Desc("Weapon to use for explosion.")]
		public readonly string Weapon = null;

		[Desc("At which damage state explosion will trigger.")]
		public readonly DamageState DamageState = DamageState.Heavy;

		[Desc("Should the explosion only be triggered once?")]
		public readonly bool TriggerOnlyOnce = false;

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new ExplosionOnDamageTransition(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			base.RulesetLoaded(rules, ai);

			if (string.IsNullOrEmpty(Weapon))
				return;

			var weaponToLower = Weapon.ToLowerInvariant();
			if (!rules.Weapons.TryGetValue(weaponToLower, out var weapon))
				throw new YamlException($"Weapons Ruleset does not contain an entry '{weaponToLower}'");

			WeaponInfo = weapon;
		}
	}

	public class ExplosionOnDamageTransition : ConditionalTrait<ExplosionOnDamageTransitionInfo>, INotifyDamageStateChanged
	{
		bool triggered;

		public ExplosionOnDamageTransition(ExplosionOnDamageTransitionInfo info)
			: base(info) { }

		void INotifyDamageStateChanged.DamageStateChanged(Actor self, AttackInfo e)
		{
			if (!self.IsInWorld)
				return;

			if (triggered)
				return;

			if (IsTraitDisabled)
				return;

			if (e.DamageState >= Info.DamageState && e.PreviousDamageState < Info.DamageState)
			{
				if (Info.TriggerOnlyOnce)
					triggered = true;

				// Use .FromPos since the actor might have been killed, don't use Target.FromActor
				Info.WeaponInfo.Impact(Target.FromPos(self.CenterPosition), e.Attacker);
			}
		}
	}
}
