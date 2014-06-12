#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.Common
{
	class InfiltratableInfo : TraitInfo<Infiltratable>
	{
		public string Type = null;
	}

	class Infiltratable { }

	class InfiltratesInfo : ITraitInfo
	{
		public string[] Types = { "Cash", "SupportPower", "Exploration" };

		public object Create(ActorInitializer init) { return new Infiltrates(this); }
	}
	
	class Infiltrates : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public readonly InfiltratesInfo Info;

		public Infiltrates(InfiltratesInfo info)
		{
			Info = info;
		}

		public IEnumerable<IOrderTargeter> Orders { get { yield return new InfiltratorOrderTargeter(Info.Types); } }
		
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

			if (order.ExtraData != 0)
			{
				// Targeted an actor under the fog
				var frozenLayer = self.Owner.PlayerActor.TraitOrDefault<FrozenActorLayer>();
				if (frozenLayer == null)
					return false;

				var frozen = frozenLayer.FromID(order.ExtraData);
				if (frozen == null)
					return false;

				var ii = frozen.Info.Traits.GetOrDefault<InfiltratableInfo>();
				return ii != null && Info.Types.Contains(ii.Type);
			}

			var i = order.TargetActor.Info.Traits.GetOrDefault<InfiltratableInfo>();
			return i != null && Info.Types.Contains(i.Type);
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
			self.QueueActivity(new Enter(target.Actor, new Infiltrate(target.Actor)));
		}

		class InfiltratorOrderTargeter : UnitOrderTargeter
		{
			string[] infiltrationTypes;

			public InfiltratorOrderTargeter(string[] infiltrationTypes)
				: base("Infiltrate", 7, "enter", true, false)
			{
				ForceAttack = false;
				this.infiltrationTypes = infiltrationTypes;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var info = target.Info.Traits.GetOrDefault<InfiltratableInfo>();
				if (info == null)
					return false;

				if (!infiltrationTypes.Contains(info.Type))
					cursor = "enter-blocked";

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var info = target.Info.Traits.GetOrDefault<InfiltratableInfo>();
				if (info == null)
					return false;

				if (!infiltrationTypes.Contains(info.Type))
					cursor = "enter-blocked";

				return true;
			}
		}
	}
}
