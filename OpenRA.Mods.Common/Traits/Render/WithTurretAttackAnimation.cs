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
	public class WithTurretAttackAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteTurretInfo>, Requires<ArmamentInfo>
	{
		[Desc("Armament name")]
		public readonly string Armament = "primary";

		[Desc("Turret name")]
		public readonly string Turret = "primary";

		[SequenceReference]
		[Desc("Displayed while attacking.")]
		public readonly string Sequence = null;

		[Desc("Delay in ticks before animation starts, either relative to attack preparation or attack.")]
		public readonly int Delay = 0;

		[Desc("Should the animation be delayed relative to preparation or actual attack?")]
		public readonly AttackDelayType DelayRelativeTo = AttackDelayType.Preparation;

		public override object Create(ActorInitializer init) { return new WithTurretAttackAnimation(init, this); }
	}

	public class WithTurretAttackAnimation : ConditionalTrait<WithTurretAttackAnimationInfo>, ITick, INotifyAttack
	{
		readonly WithSpriteTurret wst;
		readonly Armament armament;
		int tick;

		public WithTurretAttackAnimation(ActorInitializer init, WithTurretAttackAnimationInfo info)
			: base(info)
		{
			armament = init.Self.TraitsImplementing<Armament>()
				.Single(a => a.Info.Name == info.Armament);
			wst = init.Self.TraitsImplementing<WithSpriteTurret>()
				.Single(st => st.Info.Turret == info.Turret);
		}

		void PlayAttackAnimation(Actor self)
		{
			if (!string.IsNullOrEmpty(Info.Sequence))
				wst.PlayCustomAnimation(self, Info.Sequence);
		}

		void NotifyAttack(Actor self)
		{
			if (Info.Delay > 0)
				tick = Info.Delay;
			else
				PlayAttackAnimation(self);
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (!IsTraitDisabled && a == armament && Info.DelayRelativeTo == AttackDelayType.Attack)
				NotifyAttack(self);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (!IsTraitDisabled && a == armament && Info.DelayRelativeTo == AttackDelayType.Preparation)
				NotifyAttack(self);
		}

		void ITick.Tick(Actor self)
		{
			if (!IsTraitDisabled && Info.Delay > 0 && --tick == 0)
				PlayAttackAnimation(self);
		}
	}
}
