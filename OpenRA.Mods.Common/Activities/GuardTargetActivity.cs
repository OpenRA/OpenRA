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
using OpenRA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class GuardTargetActivity : Activity
	{
		Target target;

		public GuardTargetActivity(Actor self, Target target, Activity inner)
		{
			this.target = target;
			ChildActivity = inner;
		}

		public override Activity Tick(Actor self)
		{
			if (ChildActivity == null)
				return NextActivity;
			return ChildActivity;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			return ChildActivity.GetTargets(self);
		}

		public override TargetLineNode? TargetLineNode(Actor self)
		{
			return new TargetLineNode(target, Color.Yellow, false);
		}
	}
}
