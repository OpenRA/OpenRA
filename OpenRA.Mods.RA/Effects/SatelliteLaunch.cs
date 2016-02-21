#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class SatelliteLaunch : IEffect
	{
		readonly GpsPowerInfo info;
		readonly Animation doors;
		readonly WPos pos;
		int frame = 0;

		public SatelliteLaunch(Actor a, GpsPowerInfo info)
		{
			this.info = info;

			doors = new Animation(a.World, info.DoorImage);
			doors.PlayThen(info.DoorSequence,
				() => a.World.AddFrameEndTask(w => w.Remove(this)));

			pos = a.CenterPosition;
		}

		public void Tick(World world)
		{
			doors.Tick();

			if (++frame == 19)
				world.AddFrameEndTask(w => w.Add(new GpsSatellite(world, pos, info)));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return doors.Render(pos, wr.Palette(info.DoorPalette));
		}
	}
}
