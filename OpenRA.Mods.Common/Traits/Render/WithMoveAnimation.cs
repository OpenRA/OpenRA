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
	public class WithMoveAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<IMoveInfo>
	{
		[Desc("Displayed while moving.")]
		[SequenceReference] public readonly string MoveSequence = "move";

		[Desc("Which sprite body to modify.")]
		public readonly string Body = "body";

		public override object Create(ActorInitializer init) { return new WithMoveAnimation(init, this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			var matches = ai.TraitInfos<WithSpriteBodyInfo>().Count(w => w.Name == Body);
			if (matches != 1)
				throw new YamlException("WithMoveAnimation needs exactly one sprite body with matching name.");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class WithMoveAnimation : ConditionalTrait<WithMoveAnimationInfo>, ITick
	{
		readonly IMove movement;
		readonly WithSpriteBody wsb;

		public WithMoveAnimation(ActorInitializer init, WithMoveAnimationInfo info)
			: base(info)
		{
			movement = init.Self.Trait<IMove>();
			wsb = init.Self.TraitsImplementing<WithSpriteBody>().First(w => w.Info.Name == Info.Body);
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || wsb.IsTraitDisabled)
				return;

			var isMoving = movement.IsMoving && !self.IsDead;

			if (isMoving ^ (wsb.DefaultAnimation.CurrentSequence.Name != Info.MoveSequence))
				return;

			wsb.DefaultAnimation.ReplaceAnim(isMoving ? Info.MoveSequence : wsb.Info.Sequence);
		}
	}
}
