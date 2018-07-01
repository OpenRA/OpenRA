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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Turn : Activity
	{
		readonly IDisabledTrait disablable;
		readonly IFacing facing;
		readonly Mobile mobile;
		readonly int desiredFacing;
		readonly bool setIsMoving;

		public Turn(Actor self, int desiredFacing, bool setIsMoving = false, bool isInterruptible = true)
		{
			disablable = self.TraitOrDefault<IMove>() as IDisabledTrait;
			facing = self.Trait<IFacing>();
			this.desiredFacing = desiredFacing;
			this.setIsMoving = setIsMoving;
			IsInterruptible = isInterruptible;

			// This might look confusing, but the current implementation of Mobile is both IMove and IDisabledTrait,
			// and this way we can save a separate Mobile trait look-up.
			mobile = disablable as Mobile;
		}

		protected override void OnFirstRun(Actor self)
		{
			if (setIsMoving && mobile != null && !mobile.IsMoving)
				mobile.IsMoving = true;
		}

		public override Activity Tick(Actor self)
		{
			if (IsInterruptible && IsCanceled)
				return NextActivity;

			if (disablable != null && disablable.IsTraitDisabled)
				return this;

			if (desiredFacing == facing.Facing)
				return NextActivity;

			facing.Facing = Util.TickFacing(facing.Facing, desiredFacing, facing.TurnSpeed);

			return this;
		}

		protected override void OnLastRun(Actor self)
		{
			// If Mobile.IsMoving was set to 'true' earlier, we want to reset it to 'false' before the next tick.
			if (mobile != null && mobile.IsMoving)
				mobile.IsMoving = false;
		}
	}
}
