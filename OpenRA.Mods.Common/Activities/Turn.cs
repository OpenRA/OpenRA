#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class Turn : Activity
	{
		int desiredFacing;

		public Turn( int desiredFacing )
		{
			this.desiredFacing = desiredFacing;
		}

		public override Activity Tick( Actor self )
		{
			if (IsCanceled) return NextActivity;
			var facing = self.Trait<IFacing>();

			if( desiredFacing == facing.Facing )
				return NextActivity;
			facing.Facing = Util.TickFacing(facing.Facing, desiredFacing, facing.ROT);

			return this;
		}
	}
}
