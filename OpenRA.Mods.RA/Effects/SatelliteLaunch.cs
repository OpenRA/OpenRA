#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
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
		Animation doors = new Animation("atek");
		float2 doorOffset = new float2(-4,0);
		float2 pos;

		public SatelliteLaunch(Actor a)
		{
			doors.PlayThen("active",
				() => a.World.AddFrameEndTask(w => w.Remove(this)));

			pos = a.CenterLocation.ToFloat2() - .5f * doors.Image.size + doorOffset;
		}

		public void Tick( World world )
		{
			doors.Tick();

			if (++frame == 19)
			{
				world.AddFrameEndTask(w => w.Add(new GpsSatellite(pos)));
			}
		}

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			yield return new Renderable(doors.Image, pos, wr.Palette("effect"), (int)doorOffset.Y);
		}
	}
}
