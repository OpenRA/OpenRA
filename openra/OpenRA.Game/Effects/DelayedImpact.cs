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
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Effects
{
	public class DelayedImpact : IEffect
	{
		readonly Target target;
		readonly IWarhead wh;
		readonly WarheadArgs args;

		int delay;

		public DelayedImpact(int delay, IWarhead wh, Target target, WarheadArgs args)
		{
			this.wh = wh;
			this.delay = delay;
			this.target = target;
			this.args = args;
		}

		public void Tick(World world)
		{
			if (--delay <= 0)
				world.AddFrameEndTask(w => { w.Remove(this); wh.DoImpact(target, args); });
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr) { yield break; }
	}
}
