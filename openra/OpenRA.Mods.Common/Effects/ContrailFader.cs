#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Effects;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;

namespace OpenRA.Mods.Common.Effects
{
	public class ContrailFader : IEffect
	{
		readonly WPos pos;
		readonly ContrailRenderable trail;
		int ticks;

		public ContrailFader(WPos pos, ContrailRenderable trail)
		{
			this.pos = pos;
			this.trail = trail;
		}

		public void Tick(World world)
		{
			if (ticks++ == trail.Length)
				world.AddFrameEndTask(w => w.Remove(this));

			trail.Update(pos);
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr)
		{
			yield return trail;
		}
	}
}
