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

using System;
using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.Effects
{
	public class Smoke : IEffect
	{
		readonly World world;
		readonly WPos pos;
		readonly Animation anim;
		readonly string palette;

		public Smoke(World world, WPos pos, string trail, string palette, string sequence)
			: this(world, pos, () => 0, trail, palette, sequence) { }

		public Smoke(World world, WPos pos, Func<int> facingFunc, string trail, string palette, string sequence)
		{
			this.world = world;
			this.pos = pos;
			this.palette = palette;

			anim = new Animation(world, trail, facingFunc);
			anim.PlayThen(sequence,
				() => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world) { anim.Tick(); }

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (world.FogObscures(pos))
				return SpriteRenderable.None;

			return anim.Render(pos, wr.Palette(palette));
		}
	}
}
