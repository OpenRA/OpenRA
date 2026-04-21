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
	public class WithAimAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<AttackBaseInfo>
	{
		[Desc("Armament name")]
		public readonly string Armament = "primary";

		[SequenceReference]
		[FieldLoader.Require]
		[Desc("Displayed while targeting.")]
		public readonly string Sequence = null;

		[Desc("Which sprite body to modify.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithAimAnimation(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var match = ai.TraitInfos<WithSpriteBodyInfo>().SingleOrDefault(w => w.Name == Body);
			if (match == null)
				throw new YamlException("WithAimAnimation needs exactly one sprite body with matching name.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class WithAimAnimation : ConditionalTrait<WithAimAnimationInfo>, INotifyAiming
	{
		readonly AttackBase[] attackBases;
		readonly WithSpriteBody wsb;

		public WithAimAnimation(ActorInitializer init, WithAimAnimationInfo info)
			: base(info)
		{
			attackBases = init.Self.TraitsImplementing<AttackBase>().ToArray();
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().First(w => w.Info.Name == Info.Body);
		}

		void UpdateSequence(bool isAiming)
		{
			var seq = !IsTraitDisabled && isAiming ? Info.Sequence : wsb.Info.Sequence;
			wsb.DefaultAnimation.ReplaceAnim(seq);
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
