#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Turns actor randomly when idle.")]
	class TurnOnIdleInfo : ConditionalTraitInfo, Requires<MobileInfo>
	{
		[Desc("Minimum amount of ticks the actor will wait before the turn.")]
		public readonly int MinDelay = 400;

		[Desc("Maximum amount of ticks the actor will wait before the turn.")]
		public readonly int MaxDelay = 800;

		public override object Create(ActorInitializer init) { return new TurnOnIdle(init, this); }
	}

	class TurnOnIdle : ConditionalTrait<TurnOnIdleInfo>, INotifyIdle
	{
		int currentDelay;
		WAngle targetFacing;
		readonly Mobile mobile;

		public TurnOnIdle(ActorInitializer init, TurnOnIdleInfo info)
			: base(info)
		{
			currentDelay = init.World.SharedRandom.Next(Info.MinDelay, Info.MaxDelay);
			mobile = init.Self.Trait<Mobile>();
			targetFacing = mobile.Facing;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			if (mobile.IsTraitDisabled || mobile.IsTraitPaused)
				return;

			if (--currentDelay > 0)
				return;

			if (targetFacing == mobile.Facing)
			{
				targetFacing = new WAngle(self.World.SharedRandom.Next(1024));
				currentDelay = self.World.SharedRandom.Next(Info.MinDelay, Info.MaxDelay);
			}

			mobile.Facing = Util.TickFacing(mobile.Facing, targetFacing, mobile.TurnSpeed);
		}
	}
}
