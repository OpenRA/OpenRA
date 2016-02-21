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

using System.Collections.Generic;
using System.Drawing;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can enter a BridgeHut to trigger a repair.")]
	class RepairsBridgesInfo : ITraitInfo
	{
		[VoiceReference] public readonly string Voice = "Action";

		[Desc("Behaviour when entering the structure.",
			"Possible values are Exit, Suicide, Dispose.")]
		public readonly EnterBehaviour EnterBehaviour = EnterBehaviour.Dispose;

		[Desc("Cursor to use when targeting a BridgeHut of an unrepaired bridge.")]
		public readonly string TargetCursor = "goldwrench";

		[Desc("Cursor to use when repairing is denied.")]
		public readonly string TargetBlockedCursor = "goldwrench-blocked";

		[Desc("Speech notification to play when a bridge is repaired.")]
		public readonly string RepairNotification = null;

		public object Create(ActorInitializer init) { return new RepairsBridges(this); }
	}

	class RepairsBridges : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly RepairsBridgesInfo info;

		public RepairsBridges(RepairsBridgesInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new RepairBridgeOrderTargeter(info); }
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

			var hut = order.TargetActor.TraitOrDefault<BridgeHut>();
			if (hut == null)
				return null;

			return hut.BridgeDamageState == DamageState.Undamaged || hut.Repairing || hut.Bridge.IsDangling ? null : info.Voice;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "RepairBridge")
			{
				var hut = order.TargetActor.TraitOrDefault<BridgeHut>();
				if (hut == null)
					return;

				if (hut.BridgeDamageState == DamageState.Undamaged || hut.Repairing || hut.Bridge.IsDangling)
					return;

				self.SetTargetLine(Target.FromOrder(self.World, order), Color.Yellow);

				self.CancelActivity();
				self.QueueActivity(new RepairBridge(self, order.TargetActor, info.EnterBehaviour, info.RepairNotification));
			}
		}

		class RepairBridgeOrderTargeter : UnitOrderTargeter
		{
			readonly RepairsBridgesInfo info;

			public RepairBridgeOrderTargeter(RepairsBridgesInfo info)
				: base("RepairBridge", 6, info.TargetCursor, true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var hut = target.TraitOrDefault<BridgeHut>();
				if (hut == null)
					return false;

				// Require force attack to heal partially damaged bridges to avoid unnecessary cursor noise
				var damage = hut.BridgeDamageState;
				if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && damage != DamageState.Dead)
					return false;

				// Obey force moving onto bridges
				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				// Can't repair a bridge that is undamaged, already under repair, or dangling
				if (damage == DamageState.Undamaged || hut.Repairing || hut.Bridge.IsDangling)
					cursor = info.TargetBlockedCursor;

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
