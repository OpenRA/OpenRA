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

namespace OpenRA.Mods.RA.Effects
{
	class GpsSatellite : IEffect
	{
		WPos Pos;
		Animation Anim = new Animation("sputnik");

		public GpsSatellite(WPos pos)
		{
			Pos = pos;
			Anim.PlayRepeating("idle");
		}

		public void Tick( World world )
		{
			Anim.Tick();
			Pos += new WVec(0, 0, 427);

			if (Pos.Z > Pos.Y)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return Anim.Render(Pos, wr.Palette("effect"));
		}
	}
}
