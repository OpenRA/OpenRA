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
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	class C4DemolitionInfo : ITraitInfo
	{
		[Desc("Delay to demolish the target once the C4 is planted." +
			"Measured in game ticks. Default is 1.8 seconds.")]
		public readonly int C4Delay = 45;
		[Desc("Number of times to flash the target")]
		public readonly int Flashes = 3;
		[Desc("Delay before the flashing starts")]
		public readonly int FlashesDelay = 4;
		[Desc("Interval between each flash")]
		public readonly int FlashInterval = 4;
		[Desc("Duration of each flash")]
		public readonly int FlashDuration = 3;
		[Desc("Voice string when planting explosive charges.")]
		public readonly string Voice = "Attack";

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
			self.QueueActivity(new Demolish(self,
				target.Actor, info.C4Delay, info.Flashes, info.FlashesDelay, info.FlashInterval, info.FlashDuration));
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "C4" ? info.Voice : null;
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

				return target.TraitsImplementing<IDemolishable>().Any(i => i.IsValidTarget(target, self));
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				return target.Info.Traits.WithInterface<IDemolishableInfo>().Any(i => i.IsValidTarget(target.Info, self));
			}
		}
	}
}
