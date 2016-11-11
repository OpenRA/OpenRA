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
		readonly Actor target;
		readonly LegacyBridgeHut legacyHut;
		readonly BridgeHut hut;
		readonly string notification;

		public RepairBridge(Actor self, Actor target, EnterBehaviour enterBehaviour, string notification)
			: base(self, target, enterBehaviour)
		{
			this.target = target;
			legacyHut = target.TraitOrDefault<LegacyBridgeHut>();
			hut = target.TraitOrDefault<BridgeHut>();
			this.notification = notification;
		}

		protected override bool CanReserve(Actor self)
		{
			if (legacyHut != null)
				return legacyHut.BridgeDamageState != DamageState.Undamaged && !legacyHut.Repairing && legacyHut.Bridge.GetHut(0) != null && legacyHut.Bridge.GetHut(1) != null;

			if (hut != null)
				return hut.BridgeDamageState != DamageState.Undamaged && !hut.Repairing;

			return false;
		}

		protected override void OnInside(Actor self)
		{
			if (legacyHut != null)
			{
				if (legacyHut.BridgeDamageState == DamageState.Undamaged || legacyHut.Repairing || legacyHut.Bridge.GetHut(0) == null || legacyHut.Bridge.GetHut(1) == null)
					return;

				legacyHut.Repair(self);
			}
			else
			{
				if (hut.BridgeDamageState == DamageState.Undamaged || hut.Repairing)
					return;

				hut.Repair(target, self);
			}

			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", notification, self.Owner.Faction.InternalName);
		}
	}
}
