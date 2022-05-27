#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;

namespace OpenRA.Activities
{
	public class CallFunc : Activity
	{
		public CallFunc(Action a) { this.a = a; }
		public CallFunc(Action a, bool interruptible)
		{
			this.a = a;
			IsInterruptible = interruptible;
		}

		readonly Action a;

		public override bool Tick(Actor self)
		{
			a.Invoke();
			return true;
		}
	}
}
