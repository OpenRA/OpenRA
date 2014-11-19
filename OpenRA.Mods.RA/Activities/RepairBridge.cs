#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class RepairBridge : Enter
	{
		readonly BridgeHut hut;

		public RepairBridge(Actor self, Actor target)
			: base(self, target)
		{
			hut = target.Trait<BridgeHut>();
		}

		protected override bool CanReserve(Actor self)
		{
			return hut.BridgeDamageState != DamageState.Undamaged && !hut.Repairing && hut.Bridge.GetHut(0) != null && hut.Bridge.GetHut(1) != null;
		}

		protected override void OnInside(Actor self)
		{
			if (hut.BridgeDamageState == DamageState.Undamaged || hut.Repairing || hut.Bridge.GetHut(0) == null || hut.Bridge.GetHut(1) == null)
				return;
			hut.Repair(self);
			self.Destroy();
		}
	}
}
