#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
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
	[Desc("Can instantly repair other actors, but gets consumed afterwards.")]
	class EngineerRepairInfo : ITraitInfo
	{
		[VoiceReference] public readonly string Voice = "Action";

		[Desc("Behaviour when entering the structure.",
			"Possible values are Exit, Suicide, Dispose.")]
		public readonly EnterBehaviour EnterBehaviour = EnterBehaviour.Dispose;

		[Desc("What diplomatic stances allow target to be repaired by this actor.")]
		public readonly Stance ValidStances = Stance.Ally;

		public object Create(ActorInitializer init) { return new EngineerRepair(init, this); }
	}

	class EngineerRepair : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly EngineerRepairInfo info;

		public EngineerRepair(ActorInitializer init, EngineerRepairInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new EngineerRepairOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "EngineerRepair")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		static bool IsValidOrder(Actor self, Order order)
		{
			if (order.Target.Type == TargetType.FrozenActor)
				return order.Target.FrozenActor.DamageState > DamageState.Undamaged;

			if (order.Target.Type == TargetType.Actor)
				return order.TargetActor.GetDamageState() > DamageState.Undamaged;

			return false;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EngineerRepair" && IsValidOrder(self, order)
				? info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EngineerRepair" || !IsValidOrder(self, order))
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Yellow);
			self.QueueActivity(new RepairBuilding(self, target.Actor, info.EnterBehaviour, info.ValidStances));
		}

		class EngineerRepairOrderTargeter : UnitOrderTargeter
		{
			public EngineerRepairOrderTargeter()
				: base("EngineerRepair", 6, "goldwrench", false, true) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (!target.Info.HasTraitInfo<EngineerRepairableInfo>())
					return false;

				if (self.Owner.Stances[target.Owner] != Stance.Ally)
					return false;

				if (target.GetDamageState() == DamageState.Undamaged)
					cursor = "goldwrench-blocked";

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				if (!target.Info.HasTraitInfo<EngineerRepairableInfo>())
					return false;

				if (self.Owner.Stances[target.Owner] != Stance.Ally)
					return false;

				if (target.DamageState == DamageState.Undamaged)
					cursor = "goldwrench-blocked";

				return true;
			}
		}
	}

	[Desc("Eligible for instant repair.")]
	class EngineerRepairableInfo : TraitInfo<EngineerRepairable> { }

	class EngineerRepairable { }
}
