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
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Effects
{
	public class DelayedImpact : IEffect
	{
		readonly Target target;
		readonly Actor firedBy;
		readonly IEnumerable<int> damageModifiers;
		readonly IWarhead wh;

		int delay;

		public DelayedImpact(int delay, IWarhead wh, Target target, Actor firedBy, IEnumerable<int> damageModifiers)
		{
			this.wh = wh;
			this.delay = delay;

			this.target = target;
			this.firedBy = firedBy;
			this.damageModifiers = damageModifiers;
		}

		public void Tick(World world)
		{
			if (--delay <= 0)
				world.AddFrameEndTask(w => { w.Remove(this); wh.DoImpact(target, firedBy, damageModifiers); });
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr) { yield break; }
	}
}