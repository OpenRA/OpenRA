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
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class ResupplyAircraft : Activity
	{
		public override Activity Tick(Actor self)
		{
			var aircraft = self.Trait<Aircraft>();
			var host = aircraft.GetActorBelow();

			if (host == null)
				return NextActivity;

			return Util.SequenceActivities(
				aircraft.GetResupplyActivities(host).Append(NextActivity).ToArray());
		}
	}
}
