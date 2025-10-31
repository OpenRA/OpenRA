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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithTurretAimAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteTurretInfo>, Requires<AttackBaseInfo>
	{
		[Desc("Armament name")]
		public readonly string Armament = "primary";

		[Desc("Turret name")]
		public readonly string Turret = "primary";

		[SequenceReference]
		[FieldLoader.Require]
		[Desc("Displayed while targeting.")]
		public readonly string Sequence = null;

		public override object Create(ActorInitializer init) { return new WithTurretAimAnimation(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var match = ai.TraitInfos<WithSpriteTurretInfo>().SingleOrDefault(w => w.Turret == Turret);
			if (match == null)
				throw new YamlException("WithTurretAimAnimation needs exactly one sprite turret with matching Turret name.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class WithTurretAimAnimation : ConditionalTrait<WithTurretAimAnimationInfo>, INotifyAiming
	{
		readonly AttackBase[] attackBases;
		readonly WithSpriteTurret wst;

		public WithTurretAimAnimation(ActorInitializer init, WithTurretAimAnimationInfo info)
			: base(info)
		{
			attackBases = init.Self.TraitsImplementing<AttackBase>().ToArray();
			wst = init.Self.TraitsImplementing<WithSpriteTurret>().Single(st => st.Info.Turret == info.Turret);
		}

		void UpdateSequence(bool isAiming)
		{
			var seq = !IsTraitDisabled && isAiming ? Info.Sequence : wst.Info.Sequence;
			wst.DefaultAnimation.ReplaceAnim(seq);
		}

		void INotifyAiming.StartedAiming(Actor self, AttackBase ab)
		{
			// Ignore any notifications from INotifyAiming while this trait is disabled
			// otherwise we replace the current animation without being active
			if (IsTraitDisabled)
				return;

			// We know that at least one AttackBase is aiming
			UpdateSequence(true);
		}

		void INotifyAiming.StoppedAiming(Actor self, AttackBase ab)
		{
			// Ignore any notifications from INotifyAiming while this trait is disabled
			// otherwise we replace the current animation without being active
			if (IsTraitDisabled)
				return;

			UpdateSequence(attackBases.Any(a => a.IsAiming));
		}

		protected override void TraitEnabled(Actor self) { UpdateSequence(attackBases.Any(a => a.IsAiming)); }

		protected override void TraitDisabled(Actor self)
		{
			// Stop regardless of any aiming AttackBases
			UpdateSequence(false);
		}
	}
}
