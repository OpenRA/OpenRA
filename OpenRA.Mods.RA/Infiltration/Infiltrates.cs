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
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.RA.Activities;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Infiltration
{
	class InfiltratesInfo : ITraitInfo
	{
		public readonly string[] Types = { };

		public object Create(ActorInitializer init) { return new Infiltrates(this); }
	}

	class Infiltrates : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly InfiltratesInfo info;

		public Infiltrates(InfiltratesInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new TargetTypeOrderTargeter(info.Types, "Infiltrate", 7, "enter", true, false); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "Infiltrate")
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

			ActorInfo ai;

			if (order.ExtraData != 0)
			{
				// Targeted an actor under the fog
				var frozenLayer = self.Owner.PlayerActor.TraitOrDefault<FrozenActorLayer>();
				if (frozenLayer == null)
					return false;

				var frozen = frozenLayer.FromID(order.ExtraData);
				if (frozen == null)
					return false;

				ai = frozen.Info;
			}
			else
				ai = order.TargetActor.Info;

			var i = ai.Traits.GetOrDefault<ITargetableInfo>();
			return i != null && i.GetTargetTypes().Intersect(info.Types).Any();
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Infiltrate" && IsValidOrder(self, order)
				? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "Infiltrate" || !IsValidOrder(self, order))
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Red);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Red);
			self.QueueActivity(new Enter(self, target.Actor, new Infiltrate(target.Actor)));
		}
	}
}
