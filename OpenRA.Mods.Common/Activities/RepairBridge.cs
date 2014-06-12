#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class RepairBridge : Activity
	{
		Target target;

		public RepairBridge(Actor target) { this.target = Target.FromActor(target); }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || target.Type != TargetType.Actor)
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
