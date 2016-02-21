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

using OpenRA.Mods.Common.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Leave the map when idle.")]
	class FlyAwayOnIdleInfo : TraitInfo<FlyAwayOnIdle> { }

	class FlyAwayOnIdle : INotifyIdle
	{
		public void TickIdle(Actor self)
		{
			self.QueueActivity(new FlyOffMap(self));
			self.QueueActivity(new RemoveSelf());
		}
	}
}