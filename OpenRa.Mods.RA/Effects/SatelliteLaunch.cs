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

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class SatelliteLaunch : IEffect
	{
		int frame = 0;
		Actor a;
		Animation doors = new Animation("atek");
		float2 doorOffset = new float2(-4,0);

		public SatelliteLaunch(Actor a)
		{
			this.a = a;
			doors.PlayThen("active",
				() => a.World.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world )
		{
			doors.Tick();
			
			if (++frame == 19)
			{
				world.AddFrameEndTask(w => w.Add(new GpsSatellite(a.CenterLocation - .5f * doors.Image.size + doorOffset)));
			}
		}
		
		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(doors.Image,
				a.CenterLocation - .5f * doors.Image.size + doorOffset, "effect");
		}
	}
}
