#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can enter a BridgeHut or LegacyBridgeHut to trigger a repair.")]
	class RepairsBridgesInfo : TraitInfo
	{
		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Yellow;

		[Desc("Behaviour when entering the structure.",
			"Possible values are Exit, Suicide, Dispose.")]
		public readonly EnterBehaviour EnterBehaviour = EnterBehaviour.Dispose;

		[Desc("Cursor to display when targeting an unrepaired bridge.")]
		public readonly string TargetCursor = "goldwrench";

		[Desc("Cursor to display when repairing is denied.")]
		public readonly string TargetBlockedCursor = "goldwrench-blocked";

		[NotificationReference("Speech")]
		[Desc("Speech notification to play when a bridge is repaired.")]
		public readonly string RepairNotification = null;

		public override object Create(ActorInitializer init) { return new RepairsBridges(this); }
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

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "RepairBridge")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			// TODO: Add support for FrozenActors
			if (order.OrderString != "RepairBridge" || order.Target.Type != TargetType.Actor)
				return null;

			var targetActor = order.Target.Actor;
			var legacyHut = targetActor.TraitOrDefault<LegacyBridgeHut>();
			if (legacyHut != null)
				return legacyHut.BridgeDamageState == DamageState.Undamaged || legacyHut.Repairing || legacyHut.Bridge.IsDangling ? null : info.Voice;

			var hut = targetActor.TraitOrDefault<BridgeHut>();
			if (hut != null)
				return hut.BridgeDamageState == DamageState.Undamaged || hut.Repairing ? null : info.Voice;

			return null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			// TODO: Add support for FrozenActors
			// The activity supports it, but still missing way to freeze bridge state on the hut
			if (order.OrderString == "RepairBridge" && order.Target.Type == TargetType.Actor)
			{
				var targetActor = order.Target.Actor;
				var legacyHut = targetActor.TraitOrDefault<LegacyBridgeHut>();
				var hut = targetActor.TraitOrDefault<BridgeHut>();
				if (legacyHut != null)
				{
					if (legacyHut.BridgeDamageState == DamageState.Undamaged || legacyHut.Repairing || legacyHut.Bridge.IsDangling)
						return;
				}
				else if (hut != null)
				{
					if (hut.BridgeDamageState == DamageState.Undamaged || hut.Repairing)
						return;
				}
				else
					return;

				self.QueueActivity(order.Queued, new RepairBridge(self, order.Target, info.EnterBehaviour, info.RepairNotification, info.TargetLineColor));
				self.ShowTargetLines();
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
				// Obey force moving onto bridges
				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var legacyHut = target.TraitOrDefault<LegacyBridgeHut>();
				var hut = target.TraitOrDefault<BridgeHut>();
				if (legacyHut != null)
				{
					// Require force attack to heal partially damaged bridges to avoid unnecessary cursor noise
					var damage = legacyHut.BridgeDamageState;
					if (!modifiers.HasModifier(TargetModifiers.ForceAttack) && damage != DamageState.Dead)
						return false;

					// Can't repair a bridge that is undamaged, already under repair, or dangling
					if (damage == DamageState.Undamaged || legacyHut.Repairing || legacyHut.Bridge.IsDangling)
						cursor = info.TargetBlockedCursor;
				}
				else if (hut != null)
				{
					// Require force attack to heal partially damaged bridges to avoid unnecessary cursor noise
					var damage = hut.BridgeDamageState;
					if (hut.Info.RequireForceAttackForHeal && !modifiers.HasModifier(TargetModifiers.ForceAttack) && damage != DamageState.Dead)
						return false;

					// Can't repair a bridge that is undamaged, already under repair, or dangling
					if (damage == DamageState.Undamaged || hut.Repairing)
						cursor = info.TargetBlockedCursor;
				}
				else
					return false;

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
