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
	public class WithAimAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<AttackBaseInfo>
	{
		[Desc("Armament name")]
		public readonly string Armament = "primary";

		[Desc("Displayed while targeting.")]
		[FieldLoader.Require]
		[SequenceReference] public readonly string Sequence = null;

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
		readonly AttackBase attack;
		readonly WithSpriteBody wsb;

		public WithAimAnimation(ActorInitializer init, WithAimAnimationInfo info)
			: base(info)
		{
			attack = init.Self.Trait<AttackBase>();
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().First(w => w.Info.Name == Info.Body);
		}

		protected void UpdateSequence()
		{
			var seq = !IsTraitDisabled && attack.IsAiming ? Info.Sequence : wsb.Info.Sequence;
			wsb.DefaultAnimation.ReplaceAnim(seq);
		}

		void INotifyAiming.StartedAiming(Actor self, AttackBase ab) { UpdateSequence(); }
		void INotifyAiming.StoppedAiming(Actor self, AttackBase ab) { UpdateSequence(); }
		protected override void TraitEnabled(Actor self) { UpdateSequence(); }
		protected override void TraitDisabled(Actor self) { UpdateSequence(); }
	}
}
