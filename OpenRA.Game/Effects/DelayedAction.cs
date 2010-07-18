#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Effects
{
	public class DelayedAction : IEffect
	{
		Action a;
		int delay;

		public DelayedAction(int delay, Action a)
		{
			this.a = a;
			this.delay = delay;
		}

		public void Tick( World world )
		{
			if (--delay <= 0)
				world.AddFrameEndTask(w => { w.Remove(this); a(); });
		}

		public IEnumerable<Renderable> Render() { yield break; }
	}
}
