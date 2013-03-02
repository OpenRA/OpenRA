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
	public class Smoke : IEffect
	{
		readonly PPos pos;
		readonly Animation anim;

		public Smoke(World world, PPos pos, string trail)
		{
			this.pos = pos;
			anim = new Animation(trail);
			anim.PlayThen("idle",
				() => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world )
		{
			anim.Tick();
		}

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			yield return new Renderable(anim.Image, pos.ToFloat2() - .5f * anim.Image.size,
				wr.Palette("effect"), (int)pos.Y);
		}
	}
}
