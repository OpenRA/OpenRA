#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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

		[Desc("Displayed while targeting.")]
		[FieldLoader.Require]
		[SequenceReference] public readonly string Sequence = null;

		[Desc("Priority of this animation. Will override any animation with lower priority. Needs to be higher than 0.")]
		public readonly int Priority = 1;

		public override object Create(ActorInitializer init) { return new WithTurretAimAnimation(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var match = ai.TraitInfos<WithSpriteTurretInfo>().SingleOrDefault(w => w.Turret == Turret);
			if (match == null)
				throw new YamlException("WithTurretAimAnimation needs exactly one sprite turret with matching Turret name.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class WithTurretAimAnimation : ConditionalTrait<WithTurretAimAnimationInfo>, INotifyCustomTurretAnimationFinished, INotifyAiming
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

		protected void PlayAimAnimation(Actor self)
		{
			if (!IsTraitDisabled && attack.IsAiming)
				wst.PlayCustomAnimationRepeating(self, Info.Sequence, Info.Priority);
		}

		void INotifyAiming.StartedAiming(Actor self, AttackBase ab) { PlayAimAnimation(self); }
		void INotifyAiming.StoppedAiming(Actor self, AttackBase ab) { wst.CancelCustomAnimation(self, Info.Priority); }
		protected override void TraitEnabled(Actor self) { PlayAimAnimation(self); }
		protected override void TraitDisabled(Actor self) { wst.CancelCustomAnimation(self, Info.Priority); }

		string INotifyCustomTurretAnimationFinished.Turret { get { return Info.Turret; } }

		void INotifyCustomTurretAnimationFinished.CustomTurretAnimationFinished(Actor self)
		{
			// If a different custom animation was finished and we're still aiming, resume aim animation
			if (attack.IsAiming)
				wst.PlayCustomAnimationRepeating(self, Info.Sequence, Info.Priority);
		}
	}
}
