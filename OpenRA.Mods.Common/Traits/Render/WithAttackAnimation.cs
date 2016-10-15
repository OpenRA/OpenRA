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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithAttackAnimationInfo : UpgradableTraitInfo, Requires<WithSpriteBodyInfo>, Requires<ArmamentInfo>, Requires<AttackBaseInfo>
	{
		[Desc("Armament name")]
		public readonly string Armament = "primary";

		[Desc("Displayed while attacking.")]
		[SequenceReference] public readonly string AttackSequence = null;

		[Desc("Displayed while targeting.")]
		[SequenceReference] public readonly string AimSequence = null;

		[Desc("Shown while reloading.")]
		[SequenceReference(null, true)] public readonly string ReloadPrefix = null;

		[Desc("Delay in ticks before animation starts, either relative to attack preparation or attack.")]
		public readonly int Delay = 0;

		[Desc("Should the animation be delayed relative to preparation or actual attack?")]
		public readonly AttackDelayType DelayRelativeTo = AttackDelayType.Preparation;

		public override object Create(ActorInitializer init) { return new WithAttackAnimation(init, this); }
	}

	public class WithAttackAnimation : UpgradableTrait<WithAttackAnimationInfo>, ITick, INotifyAttack
	{
		readonly AttackBase attack;
		readonly Armament armament;
		readonly WithSpriteBody[] wsbs;

		int tick;

		public WithAttackAnimation(ActorInitializer init, WithAttackAnimationInfo info)
			: base(info)
		{
			attack = init.Self.Trait<AttackBase>();
			armament = init.Self.TraitsImplementing<Armament>()
				.Single(a => a.Info.Name == info.Armament);
			wsbs = init.Self.TraitsImplementing<WithSpriteBody>().ToArray();
		}

		void PlayAttackAnimation(Actor self)
		{
			foreach (var wsb in wsbs)
				if (!wsb.IsTraitDisabled && !string.IsNullOrEmpty(Info.AttackSequence))
					wsb.PlayCustomAnimation(self, Info.AttackSequence, () => wsb.CancelCustomAnimation(self));
		}

		void PlayAimSequence()
		{
			if (string.IsNullOrEmpty(Info.AimSequence) && string.IsNullOrEmpty(Info.ReloadPrefix))
				return;

			foreach (var wsb in wsbs)
			{
				if (!wsb.IsTraitDisabled)
				{
					var sequence = wsb.Info.Sequence;
					if (!string.IsNullOrEmpty(Info.AimSequence) && attack.IsAttacking)
						sequence = Info.AimSequence;

					var prefix = (armament.IsReloading && !string.IsNullOrEmpty(Info.ReloadPrefix)) ? Info.ReloadPrefix : "";

					if (!string.IsNullOrEmpty(prefix) && sequence != (prefix + sequence))
						sequence = prefix + sequence;

					wsb.DefaultAnimation.ReplaceAnim(sequence);
				}
			}
		}

		void INotifyAttack.Attacking(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (Info.DelayRelativeTo == AttackDelayType.Attack)
			{
				if (Info.Delay > 0)
					tick = Info.Delay;
				else
					PlayAttackAnimation(self);
			}
		}

		void INotifyAttack.PreparingAttack(Actor self, Target target, Armament a, Barrel barrel)
		{
			if (Info.DelayRelativeTo == AttackDelayType.Preparation)
			{
				if (Info.Delay > 0)
					tick = Info.Delay;
				else
					PlayAttackAnimation(self);
			}
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (Info.Delay > 0 && --tick == 0)
				PlayAttackAnimation(self);

			PlayAimSequence();
		}
	}
}
