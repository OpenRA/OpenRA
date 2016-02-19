#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.Common.Effects
{
	public class ContrailFader : IEffect
	{
		WPos pos;
		ContrailRenderable trail;
		int ticks;
		Rectangle bounds;

		public ContrailFader(WPos pos, ContrailRenderable trail)
		{
			this.pos = pos;
			this.trail = trail;

			bounds = new Rectangle(0, 0, trail.Length, trail.Length);
		}

		public void Tick(World world)
		{
			if (ticks == 0)
				world.ScreenMap.Add(this, pos, bounds);

			if (ticks++ == trail.Length)
				world.AddFrameEndTask(w => { w.Remove(this); w.ScreenMap.Remove(this); });

			trail.Update(pos);
			world.ScreenMap.Update(this, pos, bounds);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield return trail;
		}
	}
}
