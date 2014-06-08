#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class BridgeHutInfo : IDemolishableInfo, ITraitInfo
	{
		public bool IsValidTarget(ActorInfo actorInfo, Actor saboteur) { return false; } // TODO: bridges don't support frozen under fog

		public object Create(ActorInitializer init) { return new BridgeHut(init); }
	}

	class BridgeHut : IDemolishable
	{
		public Bridge bridge;

		public BridgeHut(ActorInitializer init)
		{
			bridge = init.Get<ParentActorInit>().value.Trait<Bridge>();
		}

		public void Repair(Actor repairer)
		{
			bridge.Repair(repairer, true, true);
		}

		public void Demolish(Actor self, Actor saboteur)
		{
			bridge.Demolish(saboteur, true, true);
		}

		public bool IsValidTarget(Actor self, Actor saboteur)
		{
			return BridgeDamageState != DamageState.Dead;
		}

		public DamageState BridgeDamageState { get { return bridge.AggregateDamageState(); } }
	}
}
