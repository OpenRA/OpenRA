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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class RepairBridge : Enter
	{
		readonly LegacyBridgeHut legacyHut;
		readonly string notification;

		public RepairBridge(Actor self, Actor target, EnterBehaviour enterBehaviour, string notification)
			: base(self, target, enterBehaviour)
		{
			legacyHut = target.Trait<LegacyBridgeHut>();
			this.notification = notification;
		}

		protected override bool CanReserve(Actor self)
		{
			return legacyHut.BridgeDamageState != DamageState.Undamaged && !legacyHut.Repairing && legacyHut.Bridge.GetHut(0) != null && legacyHut.Bridge.GetHut(1) != null;
		}

		protected override void OnInside(Actor self)
		{
			if (legacyHut.BridgeDamageState == DamageState.Undamaged || legacyHut.Repairing || legacyHut.Bridge.GetHut(0) == null || legacyHut.Bridge.GetHut(1) == null)
				return;

			legacyHut.Repair(self);

			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", notification, self.Owner.Faction.InternalName);
		}
	}
}
