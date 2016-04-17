#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits.Render
{
	public class WithMoveAnimationInfo : ITraitInfo, Requires<WithSpriteBodyInfo>, Requires<IMoveInfo>
	{
		[Desc("Displayed while moving.")]
		[SequenceReference] public readonly string MoveSequence = "move";

		public object Create(ActorInitializer init) { return new WithMoveAnimation(init, this); }
	}

	public class WithMoveAnimation : ITick
	{
		readonly WithMoveAnimationInfo info;
		readonly IMove movement;
		readonly WithSpriteBody wsb;

		WPos cachedPosition;

		public WithMoveAnimation(ActorInitializer init, WithMoveAnimationInfo info)
		{
			this.info = info;
			movement = init.Self.Trait<IMove>();
			wsb = init.Self.Trait<WithSpriteBody>();

			cachedPosition = init.Self.CenterPosition;
		}

		public void Tick(Actor self)
		{
			var oldCachedPosition = cachedPosition;
			cachedPosition = self.CenterPosition;

			// Flying units set IsMoving whenever they are airborne, which isn't enough for our purposes
			var isMoving = movement.IsMoving && !self.IsDead && (oldCachedPosition - cachedPosition).HorizontalLengthSquared != 0;
			if (isMoving ^ (wsb.DefaultAnimation.CurrentSequence.Name != info.MoveSequence))
				return;

			wsb.DefaultAnimation.ReplaceAnim(isMoving ? info.MoveSequence : wsb.Info.Sequence);
		}
	}
}
