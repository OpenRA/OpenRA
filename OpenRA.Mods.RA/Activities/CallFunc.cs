#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class CallFunc : Activity
	{
		public CallFunc(Action a) { this.a = a; }
		public CallFunc(Action a, bool interruptable)
		{
			this.a = a;
			this.interruptable = interruptable;
		}

		Action a;
		bool interruptable;

		public override Activity Tick(Actor self)
		{
			if (a != null) a();
			return NextActivity;
		}

		public override void Cancel(Actor self)
		{
			if (interruptable)
				base.Cancel(self);
		}
	}
}
