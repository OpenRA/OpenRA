#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Teleport : Activity
	{
		CPos destination;

		public Teleport(CPos destination)
		{
			this.destination = destination;
		}

		public override Activity Tick(Actor self)
		{
			self.Trait<ITeleportable>().SetPosition(self, destination);
			return NextActivity;
		}
	}
}
