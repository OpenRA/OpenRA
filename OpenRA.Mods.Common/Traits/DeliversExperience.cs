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
	[Desc("This actor can grant experience levels equal to it's own current level via entering to other actors with the `AcceptsDeliveredExperience` trait.")]
	class DeliversExperienceInfo : ITraitInfo, Requires<GainsExperienceInfo>
	{
		[Desc("The amount of experience the donating player receives.")]
		public readonly int PlayerExperience = 0;

		[Desc("Identifier checked against AcceptsDeliveredExperience.ValidTypes. Only needed if the latter is not empty.")]
		public readonly string Type = null;

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new DeliversExperience(init, this); }
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
					yield return new DeliversExperienceOrderTargeter();
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "DeliverExperience")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return info.Voice;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeliverExperience")
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
			if (target.Type != TargetType.Actor)
				return;

			var targetGainsExperience = target.Actor.Trait<GainsExperience>();
			if (targetGainsExperience.Level == targetGainsExperience.MaxLevel)
				return;

			if (!order.Queued)
				self.CancelActivity();

			var level = gainsExperience.Level;

			self.SetTargetLine(target, Color.Yellow);
			self.QueueActivity(new DonateExperience(self, target.Actor, level, info.PlayerExperience, targetGainsExperience));
		}

		public class DeliversExperienceOrderTargeter : UnitOrderTargeter
		{
			public DeliversExperienceOrderTargeter()
				: base("DeliverExperience", 5, "enter", true, true) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (target == self)
					return false;

				var type = self.Info.TraitInfo<DeliversExperienceInfo>().Type;
				var targetInfo = target.Info.TraitInfoOrDefault<AcceptsDeliveredExperienceInfo>();
				var targetGainsExperience = target.TraitOrDefault<GainsExperience>();

				if (targetGainsExperience == null || targetInfo == null)
					return false;

				if (targetGainsExperience.Level == targetGainsExperience.MaxLevel)
					return false;

				return targetInfo.ValidStances.HasStance(target.Owner.Stances[self.Owner])
					&& (targetInfo.ValidTypes.Count == 0
						|| (!string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type)));
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				if (target.Actor == null || target.Actor == self)
					return false;

				var type = self.Info.TraitInfo<DeliversExperienceInfo>().Type;
				var targetInfo = target.Info.TraitInfoOrDefault<AcceptsDeliveredExperienceInfo>();
				var targetGainsExperience = target.Actor.TraitOrDefault<GainsExperience>();

				if (targetGainsExperience == null || targetInfo == null)
					return false;

				if (targetGainsExperience.Level == targetGainsExperience.MaxLevel)
					return false;

				return targetInfo.ValidStances.HasStance(target.Owner.Stances[self.Owner])
					&& (targetInfo.ValidTypes.Count == 0
						|| (!string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type)));
			}
		}
	}
}
