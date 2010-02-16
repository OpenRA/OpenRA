#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

namespace OpenRa.Traits.Activities
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

			if( desiredFacing == unit.Facing )
				return NextActivity;

			Util.TickFacing( ref unit.Facing, desiredFacing, self.Info.Traits.Get<UnitInfo>().ROT );
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
