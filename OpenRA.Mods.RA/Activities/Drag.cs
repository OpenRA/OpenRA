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

using System.Linq;
using OpenRA.Traits;
using OpenRA.Traits.Activities;

namespace OpenRA.Mods.RA.Activities
{
	public class Drag : IActivity
	{
		public IActivity NextActivity { get; set; }

		float2 endLocation;
		float2 startLocation;
		int length;

		public Drag(float2 start, float2 end, int length)
		{
			startLocation = start;
			endLocation = end;
			this.length = length;
		}
		
		int ticks = 0;
		public IActivity Tick( Actor self )
		{
			self.CenterLocation = float2.Lerp(startLocation, endLocation, (float)ticks/(length-1));
			
			if (++ticks >= length)
				return NextActivity;
			
			return this;
		}

		public void Cancel(Actor self) {	}
	}
}
