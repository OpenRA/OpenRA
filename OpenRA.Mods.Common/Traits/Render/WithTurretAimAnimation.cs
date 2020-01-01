#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
		readonly AttackBase attack;
		readonly WithSpriteTurret wst;

		public WithTurretAimAnimation(ActorInitializer init, WithTurretAimAnimationInfo info)
			: base(info)
		{
			attack = init.Self.Trait<AttackBase>();
			wst = init.Self.TraitsImplementing<WithSpriteTurret>()
				.Single(st => st.Info.Turret == info.Turret);
		}

		protected void UpdateSequence()
		{
			var seq = !IsTraitDisabled && attack.IsAiming ? Info.Sequence : wst.Info.Sequence;
			wst.DefaultAnimation.ReplaceAnim(seq);
		}

		void INotifyAiming.StartedAiming(Actor self, AttackBase ab) { UpdateSequence(); }
		void INotifyAiming.StoppedAiming(Actor self, AttackBase ab) { UpdateSequence(); }
		protected override void TraitEnabled(Actor self) { UpdateSequence(); }
		protected override void TraitDisabled(Actor self) { UpdateSequence(); }
	}
}
