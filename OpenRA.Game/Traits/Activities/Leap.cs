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

namespace OpenRA.Traits.Activities
{
	class Leap : IActivity
	{
		Actor target;
		float2 initialLocation;
		float t;

		const int delay = 6;

		public Leap(Actor self, Actor target)
		{
			this.target = target; 
			initialLocation = self.CenterLocation;

			self.traits.Get<RenderInfantry>().Attacking(self);
			Sound.Play("dogg5p.aud");
		}

		public IActivity NextActivity { get; set; }
		
		public IActivity Tick(Actor self)
		{
			if (target == null || !target.IsInWorld)
				return NextActivity;

			t += (1f / delay);

			self.CenterLocation = float2.Lerp(initialLocation, target.CenterLocation, t);

			if (t >= 1f)
			{
				self.traits.Get<Mobile>().TeleportTo(self, target.Location);
				target.InflictDamage(self, target.Health, null);	// kill it
				return NextActivity;
			}

			return this;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
