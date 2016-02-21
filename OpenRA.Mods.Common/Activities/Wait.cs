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
		bool interruptable = true;

		public Wait(int period) { remainingTicks = period; }
		public Wait(int period, bool interruptable)
		{
			remainingTicks = period;
			this.interruptable = interruptable;
		}

		public override Activity Tick(Actor self)
		{
			return (remainingTicks-- == 0) ? NextActivity : this;
		}

		public override void Cancel(Actor self)
		{
			if (!interruptable)
				return;

			remainingTicks = 0;
			base.Cancel(self);
		}
	}

	public class WaitFor : Activity
	{
		Func<bool> f;
		bool interruptable = true;

		public WaitFor(Func<bool> f) { this.f = f; }
		public WaitFor(Func<bool> f, bool interruptable)
		{
			this.f = f;
			this.interruptable = interruptable;
		}

		public override Activity Tick(Actor self)
		{
			return (f == null || f()) ? NextActivity : this;
		}

		public override void Cancel(Actor self)
		{
			if (!interruptable)
				return;

			f = null;
			base.Cancel(self);
		}
	}
}
