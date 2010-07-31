#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;

namespace OpenRA.Traits.Activities
{
	public class Turn : IActivity
	{
		public IActivity NextActivity { get; set; }

		int desiredFacing;

		public Turn( int desiredFacing )
		{
			this.desiredFacing = desiredFacing;
		}

		public IActivity Tick( Actor self )
		{
			var unit = self.traits.Get<Unit>();
			var ROT = self.traits.Get<IMove>().ROT;

			if( desiredFacing == unit.Facing )
				return NextActivity;

			Util.TickFacing(ref unit.Facing, desiredFacing, ROT);

			return this;
		}

		public void Cancel( Actor self )
		{
			var unit = self.traits.Get<Unit>();

			desiredFacing = unit.Facing;
			NextActivity = null;
		}
	}
}
