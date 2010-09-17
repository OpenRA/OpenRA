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
	public class Explosion : IEffect
	{
		Animation anim;
		int2 pos;

		public Explosion(World world, int2 pixelPos, string style, bool isWater)
		{
			this.pos = pixelPos;
			anim = new Animation("explosion");
			anim.PlayThen(style,
				() => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick( World world ) { anim.Tick(); }

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, "effect", (int)pos.Y);
		}

		public Player Owner { get { return null; } }
	}
}
