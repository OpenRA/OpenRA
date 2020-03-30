#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can instantly repair other actors, but gets consumed afterwards.")]
	class EngineerRepairInfo : ConditionalTraitInfo
	{
		[Desc("Uses the \"EngineerRepairable\" trait to determine repairability.")]
		public readonly BitSet<EngineerRepairType> Types = default(BitSet<EngineerRepairType>);

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Behaviour when entering the structure.",
			"Possible values are Exit, Suicide, Dispose.")]
		public readonly EnterBehaviour EnterBehaviour = EnterBehaviour.Dispose;

		[Desc("What diplomatic stances allow target to be repaired by this actor.")]
		public readonly Stance ValidStances = Stance.Ally;

		[Desc("Sound to play when repairing is done.")]
		public readonly string RepairSound = null;

		[Desc("Cursor to show when hovering over a valid actor to repair.")]
		public readonly string Cursor = "goldwrench";

		[Desc("Cursor to show when target actor has full health so it can't be repaired.")]
		public readonly string RepairBlockedCursor = "goldwrench-blocked";

		public override object Create(ActorInitializer init) { return new EngineerRepair(init, this); }
	}

	class EngineerRepair : ConditionalTrait<EngineerRepairInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		public EngineerRepair(ActorInitializer init, EngineerRepairInfo info)
			: base(info) { }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new EngineerRepairOrderTargeter(Info);
			}
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
				return order.Target.Actor.GetDamageState() > DamageState.Undamaged;

			return false;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EngineerRepair" && IsValidOrder(self, order)
				? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EngineerRepair" || !IsValidOrder(self, order))
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.QueueActivity(new RepairBuilding(self, order.Target, Info));
			self.ShowTargetLines();
		}

		class EngineerRepairOrderTargeter : UnitOrderTargeter
		{
			EngineerRepairInfo info;

			public EngineerRepairOrderTargeter(EngineerRepairInfo info)
				: base("EngineerRepair", 6, info.Cursor, true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var engineerRepairable = target.Info.TraitInfoOrDefault<EngineerRepairableInfo>();
				if (engineerRepairable == null)
					return false;

				if (!engineerRepairable.Types.IsEmpty && !engineerRepairable.Types.Overlaps(info.Types))
					return false;

				if (!info.ValidStances.HasStance(self.Owner.Stances[target.Owner]))
					return false;

				if (target.GetDamageState() == DamageState.Undamaged)
					cursor = info.RepairBlockedCursor;

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var engineerRepairable = target.Info.TraitInfoOrDefault<EngineerRepairableInfo>();
				if (engineerRepairable == null)
					return false;

				if (!engineerRepairable.Types.IsEmpty && !engineerRepairable.Types.Overlaps(info.Types))
					return false;

				if (!info.ValidStances.HasStance(self.Owner.Stances[target.Owner]))
					return false;

				if (target.DamageState == DamageState.Undamaged)
					cursor = info.RepairBlockedCursor;

				return true;
			}
		}
	}
}
