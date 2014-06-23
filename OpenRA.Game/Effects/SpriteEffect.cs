#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Graphics;

namespace OpenRA.Effects
{
	public class SpriteEffect : IEffect
	{
		string palette;
		Animation anim;
		WPos pos;

		public SpriteEffect(WPos pos, World world, string sprite, string palette)
		{
			this.pos = pos;
			this.palette = palette;
			anim = new Animation(world, sprite);
			anim.PlayThen("idle", () => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world)
		{
			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(pos, WVec.Zero, 0, wr.Palette(palette), 1f / wr.Viewport.Zoom);
		}
	}
}