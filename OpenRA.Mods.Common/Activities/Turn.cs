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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Turn : Activity
	{
		readonly Mobile mobile;
		readonly IFacing facing;
		readonly int desiredFacing;

		public Turn(Actor self, int desiredFacing)
		{
			mobile = self.TraitOrDefault<Mobile>();
			facing = self.Trait<IFacing>();
			this.desiredFacing = desiredFacing;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceling)
				return NextActivity;

			if (mobile != null && (mobile.IsTraitDisabled || mobile.IsTraitPaused))
				return this;

			if (desiredFacing == facing.Facing)
				return NextActivity;

			facing.Facing = Util.TickFacing(facing.Facing, desiredFacing, facing.TurnSpeed);

			return this;
		}
	}
}
