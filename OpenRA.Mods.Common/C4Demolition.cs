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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class C4DemolitionInfo : ITraitInfo
	{
		public readonly int C4Delay = 45; // 1.8 seconds
		public object Create(ActorInitializer init) { return new C4Demolition(this); }
	}

	class C4Demolition : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly C4DemolitionInfo info;

		public C4Demolition(C4DemolitionInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new C4DemolitionOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "C4")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "C4")
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor)
				return;

			var demolishable = target.Actor.TraitOrDefault<IDemolishable>();
			if (demolishable == null || !demolishable.IsValidTarget(target.Actor, self))
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Red);
			self.QueueActivity(new Enter(target.Actor, new Demolish(target.Actor, info.C4Delay)));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "C4" ? "Attack" : null;
		}

		class C4DemolitionOrderTargeter : UnitOrderTargeter
		{
			public C4DemolitionOrderTargeter()
				: base("C4", 6, "c4", true, false) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				// Obey force moving onto bridges
				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var demolishable = target.TraitOrDefault<IDemolishable>();
				if (demolishable == null || !demolishable.IsValidTarget(target, self))
					return false;

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				// TODO: Bridges don't yet support FrozenUnderFog.
				if (target.Actor != null && target.Actor.HasTrait<BridgeHut>())
					return false;

				return true;
			}
		}
	}
}
