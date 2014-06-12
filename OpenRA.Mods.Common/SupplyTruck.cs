#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	[Desc("Donate money to building if it has the AcceptSupplies: trait.")]
	class SupplyTruckInfo : ITraitInfo
	{
		[Desc("The amount of cash the owner of the building recieves.")]
		public readonly int Payload = 500;
		public object Create(ActorInitializer init) { return new SupplyTruck(this); }
	}

	class SupplyTruck : IIssueOrder, IResolveOrder, IOrderVoice
	{
		SupplyTruckInfo info;

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
			return "Move";
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
			self.QueueActivity(new Enter(target.Actor, new DonateSupplies(target.Actor, info.Payload)));
		}

		class SupplyTruckOrderTargeter : UnitOrderTargeter
		{
			public SupplyTruckOrderTargeter()
				: base("DeliverSupplies", 5, "enter", false, true)
			{
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				return target.HasTrait<AcceptsSupplies>();
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return target.Info.Traits.Contains<AcceptsSuppliesInfo>();
			}
		}
	}
}
