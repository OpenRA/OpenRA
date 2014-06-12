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
	public class Smoke : IEffect
	{
		readonly World world;
		readonly WPos pos;
		readonly CPos cell;
		readonly Animation anim;

		public Smoke(World world, WPos pos, string trail)
		{
			this.world = world;
			this.pos = pos;
			this.cell = pos.ToCPos();

			anim = new Animation(world, trail);
			anim.PlayThen("idle",
				() => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world) { anim.Tick(); }

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			if (world.FogObscures(cell))
				return SpriteRenderable.None;

			return anim.Render(pos, wr.Palette("effect"));
		}
	}
}
