#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Follow : Activity
	{
		Activity inner;
		Target target;

		public Follow(Actor self, Target target, WRange minRange, WRange maxRange)
		{
			this.target = target;
			inner = self.Trait<IMove>().MoveWithinRange(target, minRange, maxRange);
		}

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			// Not sequenced because we want to continue ticking inner
			// even after it returns NextActivity (in case the target moves)
			Util.RunActivity(self, inner);

			return this;
		}

		public override IEnumerable<Target> GetTargets(Actor self)
		{
			if (inner != null)
				return inner.GetTargets(self);

			return Target.None;
		}
	}
}
