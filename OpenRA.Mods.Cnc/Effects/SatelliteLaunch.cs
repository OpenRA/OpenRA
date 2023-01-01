#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.Traits;

namespace OpenRA.Mods.Cnc.Effects
{
	class SatelliteLaunch : IEffect, ISpatiallyPartitionable
	{
		readonly GpsPowerInfo info;
		readonly Actor launcher;
		readonly Animation doors;
		readonly WPos pos;
		int frame = 0;

		public SatelliteLaunch(Actor launcher, GpsPowerInfo info)
		{
			this.info = info;
			this.launcher = launcher;

			doors = new Animation(launcher.World, info.DoorImage);
			doors.PlayThen(info.DoorSequence,
				() => launcher.World.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); }));

			pos = launcher.CenterPosition;
			launcher.World.ScreenMap.Add(this, pos, doors.Image);
		}

		public void Tick(World world)
		{
			doors.Tick();
			world.ScreenMap.Update(this, pos, doors.Image);

			if (++frame == 19)
			{
				var palette = info.SatellitePaletteIsPlayerPalette ? info.SatellitePalette + launcher.Owner.InternalName : info.SatellitePalette;
				world.AddFrameEndTask(w => w.Add(new GpsSatellite(world, pos, info.SatelliteImage, info.SatelliteSequence, palette, info.RevealDelay, launcher.Owner)));
			}
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			var palette = info.DoorPaletteIsPlayerPalette ? info.DoorPalette + launcher.Owner.InternalName : info.DoorPalette;
			return doors.Render(pos, wr.Palette(palette));
		}
	}
}
