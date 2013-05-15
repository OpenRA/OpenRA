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
		readonly WPos Pos;
		readonly Animation Anim;

		public Smoke(World world, WPos pos, string trail)
		{
			Pos = pos;
			Anim = new Animation(trail);
			Anim.PlayThen("idle",
				() => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world )
		{
			Anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield return new SpriteRenderable(Anim.Image, Pos, 0, wr.Palette("effect"), 1f);
		}
	}
}
