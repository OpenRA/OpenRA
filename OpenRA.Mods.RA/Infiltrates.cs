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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;
using OpenRA.Mods.RA.Missions;

namespace OpenRA.Mods.RA
{
	class InfiltratesInfo : ITraitInfo
	{
		public string[] InfiltrateTypes = {"Cash", "SupportPower", "Exploration"};
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
				yield return new InfiltratorOrderTargeter(target => CanInfiltrate(target));
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
				self.QueueActivity(new Enter(order.TargetActor));
				self.QueueActivity(new Infiltrate(order.TargetActor));
			}
		}
		
		bool CanInfiltrate(Actor target)
		{
			if (Info.InfiltrateTypes.Contains("Cash") && target.HasTrait<InfiltrateForCash>())
				return true;

			if (Info.InfiltrateTypes.Contains("SupportPower") && target.HasTrait<InfiltrateForSupportPower>())
				return true;

			if (Info.InfiltrateTypes.Contains("Exploration") && target.HasTrait<InfiltrateForExploration>())
				return true;

			if (Info.InfiltrateTypes.Contains("MissionObjective") && target.HasTrait<InfiltrateForMissionObjective>())
				return true;

			return false;
		}

		class InfiltratorOrderTargeter : UnitTraitOrderTargeter<IAcceptInfiltrator>
		{
			readonly Func<Actor, bool> useEnterCursor;
			
			public InfiltratorOrderTargeter(Func<Actor, bool> useEnterCursor) : base("Infiltrate", 7, "enter", true, false)
			{
				ForceAttack=false;
				this.useEnterCursor = useEnterCursor;
			}
			
			public override bool CanTargetActor(Actor self, Actor target, bool forceAttack, bool forceQueued, ref string cursor)
			{
				if (!base.CanTargetActor(self, target, forceAttack, forceQueued, ref cursor))
					return false;
				
				if (!useEnterCursor(target))
					cursor = "enter-blocked";

				return true;
			}
		}
	}
}
