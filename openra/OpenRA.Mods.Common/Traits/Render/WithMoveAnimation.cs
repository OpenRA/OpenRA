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
	public class WithMoveAnimationInfo : ConditionalTraitInfo, Requires<WithSpriteBodyInfo>, Requires<IMoveInfo>
	{
		[SequenceReference]
		[Desc("Displayed while moving.")]
		public readonly string MoveSequence = "move";

		[Desc("Which sprite body to modify.")]
		public readonly string Body = "body";

		[Desc("Apply condition on listed movement types. Available options are: None, Horizontal, Vertical, Turn.")]
		public readonly MovementType ValidMovementTypes = MovementType.Horizontal;

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

		void UpdateAnimation(Actor self, MovementType types)
		{
			var playAnim = false;
			if (!IsTraitDisabled && (types & Info.ValidMovementTypes) != 0)
				playAnim = true;

			if (!playAnim && wsb.DefaultAnimation.CurrentSequence.Name == Info.MoveSequence)
			{
				wsb.CancelCustomAnimation(self);
				return;
			}

			if (playAnim && wsb.DefaultAnimation.CurrentSequence.Name != Info.MoveSequence)
				wsb.PlayCustomAnimationRepeating(self, Info.MoveSequence);
		}

		void INotifyMoving.MovementTypeChanged(Actor self, MovementType types)
		{
			UpdateAnimation(self, types);
		}

		protected override void TraitEnabled(Actor self)
		{
			// HACK: Use a FrameEndTask to avoid construction order issues with WithSpriteBody
			self.World.AddFrameEndTask(w => UpdateAnimation(self, movement.CurrentMovementTypes));
		}

		protected override void TraitDisabled(Actor self)
		{
			UpdateAnimation(self, movement.CurrentMovementTypes);
		}
	}
}
