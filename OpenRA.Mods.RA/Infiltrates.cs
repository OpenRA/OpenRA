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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
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

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new InfiltratorOrderTargeter(CanInfiltrate);
			}
		}
		
		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Infiltrate")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
			
			return null;
		}
		
		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "Infiltrate" && CanInfiltrate(order.TargetActor)) ? "Attack" : null;
		}
		
		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Infiltrate")
			{
				if (!CanInfiltrate(order.TargetActor))
					return;

				self.SetTargetLine(Target.FromOrder(order), Color.Red);
				
				self.CancelActivity();
				self.QueueActivity(new Enter(order.TargetActor, new Infiltrate(order.TargetActor)));
			}
		}
		
		bool CanInfiltrate(Actor target)
		{
			var infiltratable = target.Info.Traits.GetOrDefault<InfiltratableInfo>();
			return infiltratable != null && Info.Types.Contains(infiltratable.Type);
		}

		class InfiltratorOrderTargeter : UnitOrderTargeter
		{
			readonly Func<Actor, bool> useEnterCursor;
			
			public InfiltratorOrderTargeter(Func<Actor, bool> useEnterCursor)
				: base("Infiltrate", 7, "enter", true, false)
			{
				ForceAttack = false;
				this.useEnterCursor = useEnterCursor;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (!base.CanTargetActor(self, target, modifiers, ref cursor))
					return false;

				if (!target.HasTrait<IAcceptInfiltrator>())
					return false;

				if (!useEnterCursor(target))
					cursor = "enter-blocked";

				return true;
			}
		}
	}
}
