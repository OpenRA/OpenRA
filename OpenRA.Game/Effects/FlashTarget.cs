#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Effects
{
	public class FlashTarget : IEffect
	{
		Actor target;
		int remainingTicks = 4;

		public FlashTarget(Actor target)
		{
			this.target = target;
			foreach (var e in target.World.Effects.OfType<FlashTarget>().Where(a => a.target == target).ToArray())
				target.World.Remove(e);
		}

		public void Tick( World world )
		{
			if (--remainingTicks == 0 || !target.IsInWorld)
				world.AddFrameEndTask(w => w.Remove(this));
		}

		public IEnumerable<Renderable> Render()
		{
			if (remainingTicks % 2 == 0)
				foreach (var r in target.Render())
					yield return r.WithPalette("highlight");
		}
	}
}
