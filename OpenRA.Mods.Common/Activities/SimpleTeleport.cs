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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class SimpleTeleport : Activity
	{
		CPos destination;

		public SimpleTeleport(CPos destination) { this.destination = destination; }

		public override Activity Tick(Actor self)
		{
			self.Trait<IPositionable>().SetPosition(self, destination);
			self.Generation++;
			return NextActivity;
		}
	}
}
