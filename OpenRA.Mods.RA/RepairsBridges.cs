#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class RepairsBridgesInfo : TraitInfo<RepairsBridges> {}

	class RepairsBridges : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new RepairBridgeOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "RepairBridge")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "RepairBridge")
				return null;

			var bridge = order.TargetActor.TraitOrDefault<BridgeHut>();
			if (bridge == null)
				return null;

			return bridge.BridgeDamageState > DamageState.Undamaged ? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "RepairBridge")
			{
				var bridge = order.TargetActor.TraitOrDefault<BridgeHut>();
				if (bridge == null)
					return;

				if (bridge.BridgeDamageState == DamageState.Undamaged)
					return;

				self.SetTargetLine(Target.FromOrder(self.World, order), Color.Yellow);

				self.CancelActivity();
				self.QueueActivity(new Enter(self, order.TargetActor, new RepairBridge(order.TargetActor)));
			}
		}

		class RepairBridgeOrderTargeter : UnitOrderTargeter
		{
			public RepairBridgeOrderTargeter()
				: base("RepairBridge", 6, "goldwrench", true, true) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var bridge = target.TraitOrDefault<BridgeHut>();
				if (bridge == null)
					return false;

				// Require force attack to heal partially damaged bridges to avoid unnecessary cursor noise
				var damage = bridge.BridgeDamageState;
				if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && damage != DamageState.Dead)
					return false;

				// Obey force moving onto bridges
				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				// Can't repair an undamaged bridge
				if (damage == DamageState.Undamaged)
					cursor = "goldwrench-blocked";

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				// TODO: Bridges don't yet support FrozenUnderFog.
				return false;
			}
		}
	}
}
