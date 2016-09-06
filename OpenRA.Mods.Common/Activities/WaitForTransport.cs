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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class WaitForTransport : Activity
	{
		readonly ICallForTransport transportable;

		Activity inner;

		public WaitForTransport(Actor self, Activity innerActivity)
		{
			transportable = self.TraitOrDefault<ICallForTransport>();
			inner = innerActivity;
		}

		public override Activity Tick(Actor self)
		{
			if (inner == null)
			{
				if (transportable != null)
					transportable.MovementCancelled(self);

				return NextActivity;
			}

			inner = ActivityUtils.RunActivity(self, inner);
			return this;
		}

		public override void Cancel(Actor self)
		{
			if (inner != null)
				inner.Cancel(self);
		}
	}
}
