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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Orders;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class EngineerRepairInfo : TraitInfo<EngineerRepair> {}

	class EngineerRepair : IIssueOrder, IResolveOrder, IOrderVoice
	{
		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new EngineerRepairOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "EngineerRepair")
				return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };

			return null;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return (order.OrderString == "EngineerRepair" &&
					order.TargetActor.GetDamageState() > DamageState.Undamaged) ? "Attack" : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "EngineerRepair" &&
			    order.TargetActor.GetDamageState() > DamageState.Undamaged)
			{
				self.SetTargetLine(Target.FromOrder(order), Color.Yellow);

				self.CancelActivity();
				self.QueueActivity(new Enter(order.TargetActor, new RepairBuilding(order.TargetActor)));
			}
		}

		class EngineerRepairOrderTargeter : UnitOrderTargeter
		{
			public EngineerRepairOrderTargeter()
				: base("EngineerRepair", 6, "goldwrench", false, true) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (!base.CanTargetActor(self, target, modifiers, ref cursor))
					return false;

				if (!target.HasTrait<EngineerRepairable>())
					return false;

				if (self.Owner.Stances[target.Owner] != Stance.Ally)
					return false;

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (target.GetDamageState() == DamageState.Undamaged)
					cursor = "goldwrench-blocked";

				return true;
			}
		}
	}

	class EngineerRepairableInfo : TraitInfo<EngineerRepairable> { }

	class EngineerRepairable { }
}
