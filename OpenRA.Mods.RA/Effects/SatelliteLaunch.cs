#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
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
