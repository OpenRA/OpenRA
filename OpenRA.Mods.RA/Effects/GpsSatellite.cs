#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.RA.Traits;

namespace OpenRA.Mods.RA.Effects
{
	class GpsSatellite : IEffect
	{
		readonly Animation anim;
		readonly string palette;
		WPos pos;

		public GpsSatellite(World world, WPos pos, string image, string sequence, string palette)
		{
			this.palette = palette;
			this.pos = pos;

			anim = new Animation(world, image);
			anim.PlayRepeating(sequence);
		}

		public void Tick(World world)
		{
			anim.Tick();
			pos += new WVec(0, 0, 427);

			if (pos.Z > pos.Y)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(pos, wr.Palette(palette));
		}
	}
}
