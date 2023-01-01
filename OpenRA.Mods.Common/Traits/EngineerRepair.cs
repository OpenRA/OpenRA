#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Can instantly repair other actors, but gets consumed afterwards.")]
	public class EngineerRepairInfo : ConditionalTraitInfo
	{
		[Desc("Uses the \"EngineerRepairable\" trait to determine repairability.")]
		public readonly BitSet<EngineerRepairType> Types = default;

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Yellow;

		[Desc("Behaviour when entering the structure.",
			"Possible values are Exit, Suicide, Dispose.")]
		public readonly EnterBehaviour EnterBehaviour = EnterBehaviour.Dispose;

		[Desc("What player relationship the target's owner needs to be repaired by this actor.")]
		public readonly PlayerRelationship ValidRelationships = PlayerRelationship.Ally;

		[Desc("Sound to play when repairing is done.")]
		public readonly string RepairSound = null;

		[CursorReference]
		[Desc("Cursor to display when hovering over a valid actor to repair.")]
		public readonly string Cursor = "goldwrench";

		[CursorReference]
		[Desc("Cursor to display when target actor has full health so it can't be repaired.")]
		public readonly string RepairBlockedCursor = "goldwrench-blocked";

		public override object Create(ActorInitializer init) { return new EngineerRepair(this); }
	}

	public class EngineerRepair : ConditionalTrait<EngineerRepairInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		public EngineerRepair(EngineerRepairInfo info)
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

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != "EngineerRepair")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		static bool IsValidOrder(Order order)
		{
			if (order.Target.Type == TargetType.FrozenActor)
				return order.Target.FrozenActor.DamageState > DamageState.Undamaged;

			if (order.Target.Type == TargetType.Actor)
				return order.Target.Actor.GetDamageState() > DamageState.Undamaged;

			return false;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EngineerRepair" && IsValidOrder(order)
				? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "EngineerRepair" || !IsValidOrder(order))
				return;

			self.QueueActivity(order.Queued, new RepairBuilding(self, order.Target, Info));
			self.ShowTargetLines();
		}

		class EngineerRepairOrderTargeter : UnitOrderTargeter
		{
			readonly EngineerRepairInfo info;

			public EngineerRepairOrderTargeter(EngineerRepairInfo info)
				: base("EngineerRepair", 6, info.Cursor, true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var engineerRepairable = target.TraitOrDefault<EngineerRepairable>();
				if (engineerRepairable == null || engineerRepairable.IsTraitDisabled)
					return false;

				if (!engineerRepairable.Info.Types.IsEmpty && !engineerRepairable.Info.Types.Overlaps(info.Types))
					return false;

				if (!info.ValidRelationships.HasRelationship(target.Owner.RelationshipWith(self.Owner)))
					return false;

				if (target.GetDamageState() == DamageState.Undamaged)
					cursor = info.RepairBlockedCursor;

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				// TODO: FrozenActors don't yet have a way of caching conditions, so we can't filter disabled traits
				// This therefore assumes that all EngineerRepairable traits are enabled, which is probably wrong.
				// Actors with FrozenUnderFog should therefore not disable the EngineerRepairable trait if
				// ValidStances includes Enemy actors.
				var engineerRepairable = target.Info.TraitInfoOrDefault<EngineerRepairableInfo>();
				if (engineerRepairable == null)
					return false;

				if (!engineerRepairable.Types.IsEmpty && !engineerRepairable.Types.Overlaps(info.Types))
					return false;

				if (!info.ValidRelationships.HasRelationship(target.Owner.RelationshipWith(self.Owner)))
					return false;

				if (target.DamageState == DamageState.Undamaged)
					cursor = info.RepairBlockedCursor;

				return true;
			}
		}
	}
}
