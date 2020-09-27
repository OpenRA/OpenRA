#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	[Desc("This actor can grant experience levels equal to it's own current level via entering to other actors with the `AcceptsDeliveredExperience` trait.")]
	class DeliversExperienceInfo : TraitInfo, Requires<GainsExperienceInfo>
	{
		[Desc("The amount of experience the donating player receives.")]
		public readonly int PlayerExperience = 0;

		[Desc("Identifier checked against AcceptsDeliveredExperience.ValidTypes. Only needed if the latter is not empty.")]
		public readonly string Type = null;

		[Desc("Cursor to display when hovering over a valid actor to deliver experience to.")]
		public readonly string Cursor = "enter";

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Yellow;

		public override object Create(ActorInitializer init) { return new DeliversExperience(init, this); }
	}

	class DeliversExperience : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly DeliversExperienceInfo info;
		readonly Actor self;
		readonly GainsExperience gainsExperience;

		public DeliversExperience(ActorInitializer init, DeliversExperienceInfo info)
		{
			this.info = info;
			self = init.Self;
			gainsExperience = self.Trait<GainsExperience>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (gainsExperience.Level != 0)
					yield return new DeliversExperienceOrderTargeter(info);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != "DeliverExperience")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeliverExperience")
				return null;

			return info.Voice;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeliverExperience")
				return;

			if (order.Target.Type == TargetType.Actor)
			{
				var targetGainsExperience = order.Target.Actor.Trait<GainsExperience>();
				if (targetGainsExperience.Level == targetGainsExperience.MaxLevel)
					return;
			}
			else if (order.Target.Type != TargetType.FrozenActor)
				return;

			self.QueueActivity(order.Queued, new DonateExperience(self, order.Target, gainsExperience.Level, info.PlayerExperience, info.TargetLineColor));
			self.ShowTargetLines();
		}

		public class DeliversExperienceOrderTargeter : UnitOrderTargeter
		{
			public DeliversExperienceOrderTargeter(DeliversExperienceInfo info)
				: base("DeliverExperience", 5, info.Cursor, true, true) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (target == self)
					return false;

				var targetGainsExperience = target.TraitOrDefault<GainsExperience>();
				if (targetGainsExperience == null || targetGainsExperience.Level == targetGainsExperience.MaxLevel)
					return false;

				var targetInfo = target.Info.TraitInfoOrDefault<AcceptsDeliveredExperienceInfo>();
				if (targetInfo == null || !targetInfo.ValidRelationships.HasStance(target.Owner.RelationshipWith(self.Owner)))
					return false;

				if (targetInfo.ValidTypes.Count == 0)
					return true;

				var type = self.Info.TraitInfo<DeliversExperienceInfo>().Type;
				return !string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type);
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				if (target.Actor == null || target.Actor == self)
					return false;

				var targetGainsExperience = target.Actor.TraitOrDefault<GainsExperience>();
				if (targetGainsExperience == null || targetGainsExperience.Level == targetGainsExperience.MaxLevel)
					return false;

				var targetInfo = target.Info.TraitInfoOrDefault<AcceptsDeliveredExperienceInfo>();
				if (targetInfo == null || !targetInfo.ValidRelationships.HasStance(target.Owner.RelationshipWith(self.Owner)))
					return false;

				if (targetInfo.ValidTypes.Count == 0)
					return true;

				var type = self.Info.TraitInfo<DeliversExperienceInfo>().Type;
				return !string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type);
			}
		}
	}
}
