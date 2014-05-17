#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
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
	public class MoveFlash : IEffect
	{
		Animation anim;
		WPos pos;

		public MoveFlash(WPos pos, World world)
		{
			this.pos = pos;
			anim = new Animation(world, "moveflsh");
			anim.PlayThen("idle", () => world.AddFrameEndTask(w => w.Remove(this)));
		}

		public void Tick(World world)
		{
			anim.Tick();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			return anim.Render(pos, WVec.Zero, 0, wr.Palette("moveflash"), 1f / wr.Viewport.Zoom);
		}
	}
}