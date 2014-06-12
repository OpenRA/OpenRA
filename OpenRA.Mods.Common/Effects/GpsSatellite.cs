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

namespace OpenRA.Mods.Common.Effects
{
	class GpsSatellite : IEffect
	{
		WPos pos;
		readonly Animation anim;

		public GpsSatellite(World world, WPos pos)
		{
			this.pos = pos;

			anim = new Animation(world, "sputnik");
			anim.PlayRepeating("idle");
		}

		public void Tick( World world )
		{
			anim.Tick();
			pos += new WVec(0, 0, 427);

			if (pos.Z > pos.Y)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(pos, wr.Palette("effect"));
		}
	}
}
