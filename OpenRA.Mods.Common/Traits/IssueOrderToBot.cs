#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Flags]
	public enum TargetDistance
	{
		Closest = 0,
		Furthest = 1,
		Random = 2
	}

	[Flags]
	public enum OrderTriggers
	{
		None = 0,
		Attack = 1,
		Damage = 2,
		Heal = 4,
		Periodically = 8,
		BecomingIdle = 16
	}

	[Desc("Allow this actor to automatically issue orders to bot player and processed by " + nameof(ExternalBotOrdersManager))]
	public class IssueOrderToBotInfo : ConditionalTraitInfo
	{
		[Desc("Events leading to the actor issue order. Possible values are: None, Attack, Damage, Heal, Periodically, BecomingIdle.")]
		public readonly OrderTriggers OrderTrigger = OrderTriggers.Attack | OrderTriggers.Damage;

		[FieldLoader.Require]
		[Desc("Order name to issue.")]
		public readonly string OrderName = null;

		[Desc("Chance of the order take effect.")]
		public readonly int OrderChance = 50;

		[Desc("Order's target range. <0 means disable targeting.")]
		public readonly int TargetRangeInCell = -1;

		[Desc("Should the player be the subject of the order. It is the actor by default.", "Can use this to issue support power order.")]
		public readonly bool IsIssuerOwner = false;

		[Desc("What player relationships are affected.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Enemy;

		[Desc("What types of targets are affected.")]
		public readonly BitSet<TargetableType> ValidTargets;

		[Desc("What types of targets are unaffected.", "Overrules ValidTargets.")]
		public readonly BitSet<TargetableType> InvalidTargets;

		[Desc("Use Target's Location.")]
		public readonly bool UseTargetLocation = true;

		[Desc("Delay between two successful issued orders.")]
		public readonly int OrderInterval = 2500;

		[Desc("Should attack the furthest or closest target. Possible values are Closest, Furthest, Random",
			"Multiple values mean the distance randomizes between them")]
		public readonly TargetDistance TargetDistance = TargetDistance.Closest;

		public override object Create(ActorInitializer init) { return new IssueOrderToBot(this); }
	}

	public class IssueOrderToBot : ConditionalTrait<IssueOrderToBotInfo>, INotifyAttack, ITick, INotifyDamage,
		INotifyCreated, INotifyOwnerChanged, INotifyBecomingIdle
	{
		int orderTicks;
		ExternalBotOrdersManager orderManager;

		public IssueOrderToBot(IssueOrderToBotInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			orderManager = self.Owner.PlayerActor.Trait<ExternalBotOrdersManager>();
		}

		void TryIssueOrder(Actor self)
		{
			if (!orderManager.ManagerRunning || orderTicks > 0 || orderManager.IsTraitDisabled)
				return;

			orderManager.AddEntryFromIssueOrderToBot(self, Info);
			orderTicks = Info.OrderInterval;
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (!orderManager.ManagerRunning || IsTraitDisabled || orderManager.IsTraitDisabled)
				return;

			if (Info.OrderTrigger.HasFlag(OrderTriggers.Attack))
				TryIssueOrder(self);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void ITick.Tick(Actor self)
		{
			if (!orderManager.ManagerRunning || IsTraitDisabled || orderManager.IsTraitDisabled)
				return;

			if (--orderTicks < 0 && Info.OrderTrigger.HasFlag(OrderTriggers.Periodically))
				TryIssueOrder(self);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!orderManager.ManagerRunning || IsTraitDisabled || orderManager.IsTraitDisabled)
				return;

			if (e.Damage.Value > 0 && Info.OrderTrigger.HasFlag(OrderTriggers.Damage))
				TryIssueOrder(self);

			if (e.Damage.Value < 0 && Info.OrderTrigger.HasFlag(OrderTriggers.Heal))
				TryIssueOrder(self);
		}

		void INotifyOwnerChanged.OnOwnerChanged(Actor self, Player oldOwner, Player newOwner)
		{
			orderManager = newOwner.PlayerActor.Trait<ExternalBotOrdersManager>();
		}

		void INotifyBecomingIdle.OnBecomingIdle(Actor self)
		{
			if (!orderManager.ManagerRunning || IsTraitDisabled || orderManager.IsTraitDisabled)
				return;

			if (Info.OrderTrigger.HasFlag(OrderTriggers.BecomingIdle))
				TryIssueOrder(self);
		}
	}
}
