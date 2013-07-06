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
	public class Explosion : IEffect
	{
		Animation anim;
		WPos pos;

		public Explosion(World world, WPos pos, string style)
		{
			this.pos = pos;
			anim = new Animation("explosion");
			anim.PlayThen(style, () => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world ) { anim.Tick(); }

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield return new SpriteRenderable(anim.Image, pos, 0, wr.Palette("effect"), 1f);
		}
	}
}
