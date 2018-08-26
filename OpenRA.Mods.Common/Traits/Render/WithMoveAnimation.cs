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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithMoveAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<IMoveInfo>
	{
		[Desc("Displayed while moving.")]
		[SequenceReference] public readonly string MoveSequence = "move";

		[Desc("Which sprite body to modify.")]
		public readonly string Body = "body";

		[Desc("Play sequence on straight vertical movement as well.")]
		public readonly bool ConsiderVerticalMovement = false;

		public override object Create(ActorInitializer init) { return new WithMoveAnimation(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var matches = ai.TraitInfos<WithSpriteBodyInfo>().Count(w => w.Name == Body);
			if (matches != 1)
				throw new YamlException("WithMoveAnimation needs exactly one sprite body with matching name.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class WithMoveAnimation : ConditionalTrait<WithMoveAnimationInfo>, INotifyMoving
	{
		readonly IMove movement;
		readonly WithSpriteBody wsb;

		public WithMoveAnimation(ActorInitializer init, WithMoveAnimationInfo info)
			: base(info)
		{
			movement = init.Self.Trait<IMove>();
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().Single(w => w.Info.Name == Info.Body);
		}

		void INotifyMoving.StartedMoving(Actor self)
		{
			if (!IsTraitDisabled && !wsb.IsTraitDisabled)
				wsb.PlayCustomAnimationRepeating(self, Info.MoveSequence);
		}

		void INotifyMoving.StoppedMoving(Actor self)
		{
			if (wsb.DefaultAnimation.CurrentSequence.Name == Info.MoveSequence)
				wsb.CancelCustomAnimation(self);
		}

		void INotifyMoving.StartedMovingVertically(Actor self)
		{
			if (Info.ConsiderVerticalMovement && !IsTraitDisabled && !wsb.IsTraitDisabled)
				wsb.PlayCustomAnimationRepeating(self, Info.MoveSequence);
		}

		void INotifyMoving.StoppedMovingVertically(Actor self)
		{
			if (Info.ConsiderVerticalMovement && wsb.DefaultAnimation.CurrentSequence.Name == Info.MoveSequence)
				wsb.CancelCustomAnimation(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			if (!wsb.IsTraitDisabled && movement.IsMoving)
				wsb.PlayCustomAnimationRepeating(self, Info.MoveSequence);
		}

		protected override void TraitDisabled(Actor self)
		{
			if (wsb.DefaultAnimation.CurrentSequence.Name == Info.MoveSequence)
				wsb.CancelCustomAnimation(self);
		}
	}
}
