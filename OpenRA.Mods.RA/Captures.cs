#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	[Desc("This actor can capture other actors which have the Capturable: trait.")]
	class CapturesInfo : ITraitInfo
	{
		[Desc("Types of actors that it can capture, as long as the type also exists in the Capturable Type: trait.")]
		public readonly string[] CaptureTypes = { "building" };
		[Desc("Destroy the unit after capturing.")]
		public readonly bool ConsumeActor = false;

		public object Create(ActorInitializer init) { return new Captures(init.self, this); }
	}

	class Captures : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly CapturesInfo Info;

		public Captures(Actor self, CapturesInfo info)
		{
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new CaptureOrderTargeter();
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "CaptureActor")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		bool IsValidOrder(Actor self, Order order)
		{
			// Not targeting an actor
			if (order.ExtraData == 0 && order.TargetActor == null)
				return false;

			if (order.ExtraData != 0)
			{
				// Targeted an actor under the fog
				var frozenLayer = self.Owner.PlayerActor.TraitOrDefault<FrozenActorLayer>();
				if (frozenLayer == null)
					return false;

				var frozen = frozenLayer.FromID(order.ExtraData);
				if (frozen == null)
					return false;

				var ci = frozen.Info.Traits.GetOrDefault<CapturableInfo>();
				return ci != null && ci.CanBeTargetedBy(self, frozen.Owner);
			}

			var c = order.TargetActor.TraitOrDefault<Capturable>();
			return c != null && !c.CaptureInProgress && c.Info.CanBeTargetedBy(self, order.TargetActor.Owner);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "CaptureActor" && IsValidOrder(self, order)
				? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "CaptureActor" || !IsValidOrder(self, order))
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Red);
			self.QueueActivity(new CaptureActor(target));
		}
	}

	class CaptureOrderTargeter : UnitOrderTargeter
	{
		public CaptureOrderTargeter() : base("CaptureActor", 6, "enter", true, true) { }

		public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
		{
			var c = target.TraitOrDefault<Capturable>();

			var canTargetActor = c != null && !c.CaptureInProgress && c.Info.CanBeTargetedBy(self, target.Owner);
			cursor = canTargetActor ? "ability" : "move-blocked";
			return canTargetActor;
		}

		public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
		{
			var c = target.Info.Traits.GetOrDefault<CapturableInfo>();

			var canTargetActor = c != null && c.CanBeTargetedBy(self, target.Owner);
			cursor = canTargetActor ? "ability" : "move-blocked";
			return canTargetActor;
		}
	}
}
