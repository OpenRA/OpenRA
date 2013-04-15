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
	class RepairBridge : Activity
	{
		Target target;

		public RepairBridge(Actor target) { this.target = Target.FromActor(target); }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValid)
				return NextActivity;

			var hut = target.Actor.Trait<BridgeHut>();
			if (hut.BridgeDamageState == DamageState.Undamaged)
				return NextActivity;

			hut.Repair(self);
			self.Destroy();

			return this;
		}
	}
}
