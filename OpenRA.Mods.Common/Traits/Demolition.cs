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
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class DemolitionInfo : ConditionalTraitInfo
	{
		[Desc("Delay to demolish the target once the explosive device is planted. " +
			"Measured in game ticks. Default is 1.8 seconds.")]
		public readonly int DetonationDelay = 45;

		[Desc("Number of times to flash the target.")]
		public readonly int Flashes = 3;

		[Desc("Delay before the flashing starts.")]
		public readonly int FlashesDelay = 4;

		[Desc("Interval between each flash.")]
		public readonly int FlashInterval = 4;

		[Desc("Behaviour when entering the structure.",
			"Possible values are Exit, Suicide, Dispose.")]
		public readonly EnterBehaviour EnterBehaviour = EnterBehaviour.Exit;

		[Desc("Types of damage that this trait causes. Leave empty for no damage types.")]
		public readonly BitSet<DamageType> DamageTypes = default;

		[VoiceReference]
		[Desc("Voice string when planting explosive charges.")]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Crimson;

		public readonly PlayerRelationship TargetRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral;
		public readonly PlayerRelationship ForceTargetRelationships = PlayerRelationship.Enemy | PlayerRelationship.Neutral | PlayerRelationship.Ally;

		[CursorReference]
		[Desc("Cursor to display when hovering over a demolishable target.")]
		public readonly string Cursor = "c4";

		public override object Create(ActorInitializer init) { return new Demolition(this); }
	}

	class Demolition : ConditionalTrait<DemolitionInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		public Demolition(DemolitionInfo info)
			: base(info) { }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new DemolitionOrderTargeter(Info);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != "C4" || IsTraitDisabled)
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "C4" || IsTraitDisabled)
				return;

			if (order.Target.Type == TargetType.Actor)
			{
				var demolishables = order.Target.Actor.TraitsImplementing<IDemolishable>();
				if (!demolishables.Any(i => i.IsValidTarget(order.Target.Actor, self)))
					return;
			}

			self.QueueActivity(order.Queued, GetDemolishActivity(self, order.Target, Info.TargetLineColor));
			self.ShowTargetLines();
		}

		public Demolish GetDemolishActivity(Actor self, Target target, Color? targetLineColor)
		{
			return new Demolish(self, target, Info.EnterBehaviour, Info.DetonationDelay, Info.Flashes,
				Info.FlashesDelay, Info.FlashInterval, Info.DamageTypes, targetLineColor);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return null;

			return order.OrderString == "C4" ? Info.Voice : null;
		}

		class DemolitionOrderTargeter : UnitOrderTargeter
		{
			readonly DemolitionInfo info;

			public DemolitionOrderTargeter(DemolitionInfo info)
				: base("C4", 6, info.Cursor, true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				// Obey force moving onto bridges
				if (modifiers.HasModifier(TargetModifiers.ForceMove))
					return false;

				var relationship = target.Owner.RelationshipWith(self.Owner);
				if (!info.TargetRelationships.HasRelationship(relationship) && !modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				if (!info.ForceTargetRelationships.HasRelationship(relationship) && modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				return target.TraitsImplementing<IDemolishable>().Any(i => i.IsValidTarget(target, self));
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var relationship = target.Owner.RelationshipWith(self.Owner);
				if (!info.TargetRelationships.HasRelationship(relationship) && !modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				if (!info.ForceTargetRelationships.HasRelationship(relationship) && modifiers.HasModifier(TargetModifiers.ForceAttack))
					return false;

				return target.Info.TraitInfos<IDemolishableInfo>().Any(i => i.IsValidTarget(target.Info, self));
			}
		}
	}
}
