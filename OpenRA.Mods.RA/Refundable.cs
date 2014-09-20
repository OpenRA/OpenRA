#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("Donate money to the target actor if it has the `RefundsUnits` trait.")]
	public class RefundableInfo : ITraitInfo
	{
		[Desc("RefundsUnits.RefundTypes checks this to match types.")]
		public readonly string RefundType = null;

		[Desc("Percentage of actor production cost to refund.")]
		public readonly int RefundPercent = 50;

		public object Create(ActorInitializer init) { return new Refundable(this); }
	}

	public class Refundable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly RefundableInfo info;

		public Refundable(RefundableInfo info) { this.info = info; }

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new RefundableOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "RefundMe")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public string VoicePhraseForOrder(Actor self, Order order) { return "Move"; }

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "RefundMe")
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
			if (target.Type != TargetType.Actor)
				return;

			var targetActor = target.Actor;
			var refundsUnits = targetActor.TraitOrDefault<RefundsUnits>();
			if (refundsUnits == null)
				return;

			if (!refundsUnits.Info.RefundableTypes.Contains(info.RefundType))
				return;

			var refundAmount = refundsUnits.GetRefundValue(self);

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Yellow);
			self.QueueActivity(new Enter(targetActor, new DonateSupplies(targetActor, refundAmount, refundAmount > 0 && refundsUnits.Info.CashTick)));
		}

		class RefundableOrderTargeter : UnitOrderTargeter
		{
			public RefundableOrderTargeter() : base("RefundMe", 5, "enter", false, true) {	}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var refunder = target.Info.Traits.GetOrDefault<RefundsUnitsInfo>();
				var refundable = self.Info.Traits.Get<RefundableInfo>();

				if (refunder == null)
					return false;

				if (self.Owner == target.Owner && !refunder.AllowSelfOwned)
					return false;

				var allied = self.Owner.IsAlliedWith(target.Owner);

				if (!allied)
					return false;

				if (allied && !refunder.AllowFriendlies)
					return false;

				return refunder.RefundableTypes.Contains(refundable.RefundType);
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var refunder = target.Info.Traits.GetOrDefault<RefundsUnitsInfo>();
				var refundable = self.Info.Traits.Get<RefundableInfo>();

				if (refunder == null)
					return false;

				var allied = self.Owner.IsAlliedWith(target.Owner);

				if (!allied)
					return false;

				if (allied && !refunder.AllowFriendlies)
					return false;

				return refunder.RefundableTypes.Contains(refundable.RefundType);
			}
		}
	}

	[Desc("Overrides RefundsUnits.RefundPercent with an exact value.")]
	public class CustomRefundValueInfo : TraitInfo<CustomRefundValue>
	{
		public readonly int Value = 0;
	}

	public class CustomRefundValue { }
}
