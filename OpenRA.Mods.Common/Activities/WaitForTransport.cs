#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class WaitForTransport : Activity
	{
		readonly ICallForTransport transportable;
		readonly Activity inner;

		public WaitForTransport(Actor self, Activity inner)
		{
			transportable = self.TraitOrDefault<ICallForTransport>();
			this.inner = inner;
		}

		protected override void OnFirstRun(Actor self)
		{
			QueueChild(inner);
		}

		public override bool Tick(Actor self)
		{
			if (transportable != null)
				transportable.MovementCancelled(self);

			return true;
		}
	}
}
