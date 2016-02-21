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

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Allows bridges to be targeted for demolition and repair.")]
	class BridgeHutInfo : IDemolishableInfo, ITraitInfo
	{
		public bool IsValidTarget(ActorInfo actorInfo, Actor saboteur) { return false; } // TODO: bridges don't support frozen under fog

		public object Create(ActorInitializer init) { return new BridgeHut(init); }
	}

	class BridgeHut : IDemolishable
	{
		public readonly Bridge FirstBridge;
		public readonly Bridge Bridge;
		public DamageState BridgeDamageState { get { return Bridge.AggregateDamageState(); } }
		public bool Repairing { get { return repairDirections > 0; } }
		int repairDirections = 0;

		public BridgeHut(ActorInitializer init)
		{
			Bridge = init.Get<ParentActorInit>().ActorValue.Trait<Bridge>();
			Bridge.AddHut(this);
			FirstBridge = Bridge.Enumerate(0, true).Last();
		}

		public void Repair(Actor repairer)
		{
			repairDirections = Bridge.GetHut(0) != this && Bridge.GetHut(1) != this ? 2 : 1;
			Bridge.Do((b, d) => b.Repair(repairer, d, () => repairDirections--));
		}

		public void Demolish(Actor self, Actor saboteur)
		{
			Bridge.Do((b, d) => b.Demolish(saboteur, d));
		}

		public bool IsValidTarget(Actor self, Actor saboteur)
		{
			return BridgeDamageState != DamageState.Dead;
		}
	}
}
