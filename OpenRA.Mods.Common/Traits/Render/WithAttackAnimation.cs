#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	public class WithAttackAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<ArmamentInfo>, Requires<AttackBaseInfo>
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

		[Desc("Which sprite body to modify.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithAttackAnimation(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var matches = ai.TraitInfos<WithSpriteBodyInfo>().Count(w => w.Name == Body);
			if (matches != 1)
				throw new YamlException("WithAttackAnimation needs exactly one sprite body with matching name.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class WithAttackAnimation : ConditionalTrait<WithAttackAnimationInfo>, ITick, INotifyAttack
	{
		readonly AttackBase attack;
		readonly Armament armament;
		readonly WithSpriteBody wsb;
		readonly bool noAimOrReloadAnim;

		int tick;
		bool attackAnimPlaying;

		public WithAttackAnimation(ActorInitializer init, WithAttackAnimationInfo info)
			: base(info)
		{
			attack = init.Self.Trait<AttackBase>();
			armament = init.Self.TraitsImplementing<Armament>()
				.Single(a => a.Info.Name == Info.Armament);
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().First(w => w.Info.Name == Info.Body);

			noAimOrReloadAnim = string.IsNullOrEmpty(Info.AimSequence) && string.IsNullOrEmpty(Info.ReloadPrefix);
		}

		void PlayAttackAnimation(Actor self)
		{
			if (!IsTraitDisabled && !wsb.IsTraitDisabled && !string.IsNullOrEmpty(Info.AttackSequence))
			{
				attackAnimPlaying = true;
				wsb.PlayCustomAnimation(self, Info.AttackSequence,
					() => { wsb.CancelCustomAnimation(self); attackAnimPlaying = false; });
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
			if (Info.Delay > 0 && --tick == 0)
				PlayAttackAnimation(self);

			if (IsTraitDisabled || noAimOrReloadAnim || attackAnimPlaying || wsb.IsTraitDisabled)
				return;

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
