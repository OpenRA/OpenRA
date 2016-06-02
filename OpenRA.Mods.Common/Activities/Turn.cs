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

using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Turn : Activity
	{
		readonly IDisabledTrait disablable;
		readonly int desiredFacing;

		public Turn(Actor self, int desiredFacing)
		{
			disablable = self.TraitOrDefault<IMove>() as IDisabledTrait;
			this.desiredFacing = desiredFacing;
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;
			if (disablable != null && disablable.IsTraitDisabled)
				return this;

			var facing = self.Trait<IFacing>();

			if (desiredFacing == facing.Facing)
				return NextActivity;
			facing.Facing = Util.TickFacing(facing.Facing, desiredFacing, facing.TurnSpeed);

			return this;
		}
	}
}
