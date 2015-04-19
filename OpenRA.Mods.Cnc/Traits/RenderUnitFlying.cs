#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class RenderUnitFlyingInfo : RenderUnitInfo, Requires<IMoveInfo>
	{
		public readonly string MoveSequence = "move";

		public override object Create(ActorInitializer init) { return new RenderUnitFlying(init, this); }
	}

	class RenderUnitFlying : RenderUnit, ITick
	{
		readonly RenderUnitFlyingInfo info;
		readonly IMove movement;

		WPos cachedPosition;

		public RenderUnitFlying(ActorInitializer init, RenderUnitFlyingInfo info)
			: base(init, info)
		{
			this.info = info;
			movement = init.Self.Trait<IMove>();

			cachedPosition = init.Self.CenterPosition;
		}

		public override void Tick(Actor self)
		{
			base.Tick(self);

			var oldCachedPosition = cachedPosition;
			cachedPosition = self.CenterPosition;

			// Flying units set IsMoving whenever they are airborne, which isn't enough for our purposes
			var isMoving = movement.IsMoving && !self.IsDead && (oldCachedPosition - cachedPosition).HorizontalLengthSquared != 0;
			if (isMoving ^ (DefaultAnimation.CurrentSequence.Name != info.MoveSequence))
				return;

			DefaultAnimation.ReplaceAnim(isMoving ? info.MoveSequence : info.Sequence);
		}
	}
}
