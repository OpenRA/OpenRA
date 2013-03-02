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
	public class Corpse : IEffect
	{
		readonly Animation anim;
		readonly float2 pos;
		readonly string paletteName;

		public Corpse(World world, float2 pos, string image, string sequence, string paletteName)
		{
			this.pos = pos;
			this.paletteName = paletteName;
			anim = new Animation(image);
			anim.PlayThen(sequence, () => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world) { anim.Tick(); }

		public IEnumerable<Renderable> Render(WorldRenderer wr)
		{
			yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, wr.Palette(paletteName), (int)pos.Y);
		}
	}
}
