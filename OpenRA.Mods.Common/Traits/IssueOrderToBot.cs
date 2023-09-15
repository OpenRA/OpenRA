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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
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

	[Desc("Allow this actor to automatically issue orders to bot player, and processed by " + nameof(ExternalBotOrdersManager) + ". Only support order without target")]
	public class IssueOrderToBotInfo : ConditionalTraitInfo
	{
		[Desc("Events leading to the actor issue order. Possible values are: None, Attack, Damage, Heal, Periodically, BecomingIdle.")]
		public readonly OrderTriggers OrderTrigger = OrderTriggers.Attack | OrderTriggers.Damage;

		[FieldLoader.Require]
		[Desc("Order name to issue.")]
		public readonly string OrderName = null;

		[Desc("Second order name to issue.")]
		public readonly string SecondOrderName = null;

		[Desc("Chance of the order take effect.")]
		public readonly int OrderChance = 50;

		[Desc("Chance of the second order take effect.")]
		public readonly int SecondOrderChance = 50;

		[Desc("Delay between two successful issued orders.")]
		public readonly int OrderInterval = 2500;

		[Desc("Delay to issue second order after a first order.",
			"Note: if set > 0, next issued order will be OrderInterval + SecondOrderDelay")]
		public readonly int SecondOrderDelay = -1;

		public override object Create(ActorInitializer init) { return new IssueOrderToBot(this); }
	}

	public class IssueOrderToBot : ConditionalTrait<IssueOrderToBotInfo>, INotifyAttack, ITick, INotifyDamage, INotifyCreated, ISync, INotifyOwnerChanged, INotifyBecomingIdle
	{
		int secondOrderTicks = -1, firstOrderTicks;
		ExternalBotOrdersManager orderManager;

		public IssueOrderToBot(IssueOrderToBotInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			orderManager = self.Owner.PlayerActor.Trait<ExternalBotOrdersManager>();
		}

		void TryIssueFirstOrder(Actor self)
		{
			if (!orderManager.ManagerRunning || firstOrderTicks > 0 || orderManager.IsTraitDisabled)
				return;

			orderManager.AddEntry(self, Info.OrderName, Info.OrderChance);

			firstOrderTicks = Info.OrderInterval;
			secondOrderTicks = Info.SecondOrderDelay;
		}

		void TryIssueSecondOrder(Actor self)
		{
			if (!orderManager.ManagerRunning || orderManager.IsTraitDisabled)
				return;

			orderManager.AddEntry(self, Info.SecondOrderName, Info.SecondOrderChance);
		}

		void INotifyAttack.Attacking(Actor self, in Target target, Armament a, Barrel barrel)
		{
			if (!orderManager.ManagerRunning || IsTraitDisabled || orderManager.IsTraitDisabled)
				return;

			if (Info.OrderTrigger.HasFlag(OrderTriggers.Attack))
				TryIssueFirstOrder(self);
		}

		void INotifyAttack.PreparingAttack(Actor self, in Target target, Armament a, Barrel barrel) { }

		void ITick.Tick(Actor self)
		{
			if (!orderManager.ManagerRunning || IsTraitDisabled || orderManager.IsTraitDisabled)
				return;

			if (Info.SecondOrderDelay > -1)
			{
				if (--secondOrderTicks < 0)
					TryIssueSecondOrder(self);

				return;
			}

			if (--firstOrderTicks < 0 && Info.OrderTrigger.HasFlag(OrderTriggers.Periodically))
				TryIssueFirstOrder(self);
		}

		void INotifyDamage.Damaged(Actor self, AttackInfo e)
		{
			if (!orderManager.ManagerRunning || IsTraitDisabled || orderManager.IsTraitDisabled)
				return;

			if (e.Damage.Value > 0 && Info.OrderTrigger.HasFlag(OrderTriggers.Damage))
				TryIssueFirstOrder(self);

			if (e.Damage.Value < 0 && Info.OrderTrigger.HasFlag(OrderTriggers.Heal))
				TryIssueFirstOrder(self);
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
				TryIssueFirstOrder(self);
		}
	}
}
