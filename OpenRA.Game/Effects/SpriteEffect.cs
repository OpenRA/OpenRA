#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
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
		readonly string palette;
		readonly Animation anim;
		readonly WPos pos;

		public SpriteEffect(WPos pos, World world, string image, string palette)
		{
			this.pos = pos;
			this.palette = palette;
			anim = new Animation(world, image);
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