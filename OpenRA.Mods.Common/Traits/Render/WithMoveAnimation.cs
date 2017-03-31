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
	public class WithMoveAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<IMoveInfo>
	{
		[Desc("Displayed while moving.")]
		[SequenceReference] public readonly string MoveSequence = "move";

		[Desc("Which sprite body to modify.")]
		public readonly string[] BodyNames = { "body" };

		public object Create(ActorInitializer init) { return new WithMoveAnimation(init, this); }
	}

	public class WithMoveAnimation : ITick
	{
		readonly WithMoveAnimationInfo info;
		readonly IMove movement;
		readonly WithSpriteBody[] wsbs;

		public WithMoveAnimation(ActorInitializer init, WithMoveAnimationInfo info)
		{
			this.info = info;
			movement = init.Self.Trait<IMove>();
			wsbs = init.Self.TraitsImplementing<WithSpriteBody>().Where(w => info.BodyNames.Contains(w.Info.Name)).ToArray();
		}

		void ITick.Tick(Actor self)
		{
			var isMoving = movement.IsMoving && !self.IsDead;

			foreach (var wsb in wsbs)
			{
				if (wsb.IsTraitDisabled)
					continue;

				if (isMoving ^ (wsb.DefaultAnimation.CurrentSequence.Name != info.MoveSequence))
					continue;

				wsb.DefaultAnimation.ReplaceAnim(isMoving ? info.MoveSequence : wsb.Info.Sequence);
			}
		}
	}
}
