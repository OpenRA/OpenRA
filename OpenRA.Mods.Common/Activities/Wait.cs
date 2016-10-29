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
using OpenRA.Activities;

namespace OpenRA.Mods.Common.Activities
{
	public class Wait : Activity
	{
		int remainingTicks;

		public Wait(int period) { remainingTicks = period; }
		public Wait(int period, bool interruptable)
		{
			remainingTicks = period;
			IsInterruptible = interruptable;
		}

		public override Activity Tick(Actor self)
		{
			return (remainingTicks-- == 0) ? NextActivity : this;
		}

		public override bool Cancel(Actor self)
		{
			if (!base.Cancel(self))
				return false;

			remainingTicks = 0;
			return true;
		}
	}

	public class WaitFor : Activity
	{
		Func<bool> f;

		public WaitFor(Func<bool> f) { this.f = f; }
		public WaitFor(Func<bool> f, bool interruptable)
		{
			this.f = f;
			IsInterruptible = interruptable;
		}

		public override Activity Tick(Actor self)
		{
			return (f == null || f()) ? NextActivity : this;
		}

		public override bool Cancel(Actor self)
		{
			if (!base.Cancel(self))
				return false;

			f = null;
			return true;
		}
	}
}
