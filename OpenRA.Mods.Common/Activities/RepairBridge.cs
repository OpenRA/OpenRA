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

using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	class RepairBridge : Enter
	{
		readonly EnterBehaviour enterBehaviour;
		readonly string notification;

		Actor enterActor;
		BridgeHut enterHut;
		LegacyBridgeHut enterLegacyHut;

		public RepairBridge(Actor self, Target target, EnterBehaviour enterBehaviour, string notification)
			: base(self, target, Color.Yellow)
		{
			this.enterBehaviour = enterBehaviour;
			this.notification = notification;
		}

		bool CanEnterHut()
		{
			if (enterLegacyHut != null)
				return enterLegacyHut.BridgeDamageState != DamageState.Undamaged && !enterLegacyHut.Repairing &&
					enterLegacyHut.Bridge.GetHut(0) != null && enterLegacyHut.Bridge.GetHut(1) != null;

			if (enterHut != null)
				return enterHut.BridgeDamageState != DamageState.Undamaged && !enterHut.Repairing;

			return false;
		}

		protected override bool TryStartEnter(Actor self, Actor targetActor)
		{
			enterActor = targetActor;
			enterLegacyHut = enterActor.TraitOrDefault<LegacyBridgeHut>();
			enterHut = enterActor.TraitOrDefault<BridgeHut>();

			// Make sure we can still repair the target before entering
			// (but not before, because this may stop the actor in the middle of nowhere)
			if (!CanEnterHut())
			{
				Cancel(self, true);
				return false;
			}

			return true;
		}

		protected override void OnEnterComplete(Actor self, Actor targetActor)
		{
			// Make sure the target hasn't changed while entering
			// OnEnterComplete is only called if targetActor is alive
			if (targetActor != enterActor)
				return;

			if (!CanEnterHut())
				return;

			if (enterLegacyHut != null)
				enterLegacyHut.Repair(self);
			else if (enterHut != null)
				enterHut.Repair(enterActor, self);

			Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", notification, self.Owner.Faction.InternalName);

			if (enterBehaviour == EnterBehaviour.Dispose)
				self.Dispose();
			else if (enterBehaviour == EnterBehaviour.Suicide)
				self.Kill(self);
		}
	}
}
