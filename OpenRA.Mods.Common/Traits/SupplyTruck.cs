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
	[Desc("Donate money to actors with the `AcceptSupplies` trait.")]
	class SupplyTruckInfo : ITraitInfo
	{
		[Desc("The amount of cash the owner receives.")]
		public readonly int Payload = 500;

		[Desc("The amount of experience the donating player receives.")]
		public readonly int PlayerExperience = 0;

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new SupplyTruck(this); }
	}

	class SupplyTruck : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly SupplyTruckInfo info;

		public SupplyTruck(SupplyTruckInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new SupplyTruckOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "DeliverSupplies")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return info.Voice;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeliverSupplies")
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Yellow);
			self.QueueActivity(new DonateSupplies(self, target.Actor, info.Payload, info.PlayerExperience));
		}

		class SupplyTruckOrderTargeter : UnitOrderTargeter
		{
			public SupplyTruckOrderTargeter()
				: base("DeliverSupplies", 5, "enter", false, true)
			{
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				return target.Info.HasTraitInfo<AcceptsSuppliesInfo>();
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return target.Info.HasTraitInfo<AcceptsSuppliesInfo>();
			}
		}
	}
}
