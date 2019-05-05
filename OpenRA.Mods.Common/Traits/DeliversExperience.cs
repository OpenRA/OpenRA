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
	class DeliversExperienceInfo : ConditionalTraitInfo, Requires<GainsExperienceInfo>
	{
		[Desc("The amount of experience the donating player receives.")]
		public readonly int PlayerExperience = 0;

		[Desc("Identifier checked against AcceptsDeliveredExperience.ValidTypes. Only needed if the latter is not empty.")]
		public readonly string Type = null;

		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new DeliversExperience(init, this); }
	}

	class DeliversExperience : ConditionalTrait<DeliversExperienceInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		readonly GainsExperience gainsExperience;

		public DeliversExperience(ActorInitializer init, DeliversExperienceInfo info)
			: base(info)
		{
			self = init.Self;
			gainsExperience = self.Trait<GainsExperience>();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (!IsTraitDisabled && gainsExperience.Level != 0)
					yield return new DeliversExperienceOrderTargeter(Info);
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
			if (order.OrderString != "DeliverExperience")
				return null;

			return Info.Voice;
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

			self.QueueActivity(order.Queued, new DonateExperience(self, order.Target, gainsExperience.Level, this));
			self.ShowTargetLines();
		}

		public class DeliversExperienceOrderTargeter : UnitOrderTargeter
		{
			readonly DeliversExperienceInfo info;

			public DeliversExperienceOrderTargeter(DeliversExperienceInfo info)
				: base("DeliverExperience", 5, "enter", true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (target == self)
					return false;

				var type = info.Type;
				var ade = target.TraitOrDefault<AcceptsDeliveredExperience>();
				var targetGainsExperience = target.TraitOrDefault<GainsExperience>();

				if (targetGainsExperience == null || ade == null)
					return false;

				if (ade.IsTraitDisabled)
					return false;

				if (targetGainsExperience.Level == targetGainsExperience.MaxLevel)
					return false;

				return ade.Info.ValidStances.HasStance(target.Owner.Stances[self.Owner])
					&& (ade.Info.ValidTypes.Count == 0
						|| (!string.IsNullOrEmpty(type) && ade.Info.ValidTypes.Contains(type)));
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				if (target.Actor == null || target.Actor == self)
					return false;

				var type = info.Type;
				var ade = target.Info.TraitInfoOrDefault<AcceptsDeliveredExperienceInfo>();
				var targetGainsExperience = target.Actor.TraitOrDefault<GainsExperience>();

				if (targetGainsExperience == null || ade == null)
					return false;

				// TODO: FrozenActors don't yet have a way of caching conditions, so we can't filter disabled traits
				// This therefore assumes that all AcceptsDeliveredExperience traits are enabled, which is probably wrong.
				// Actors with FrozenUnderFog should therefore not disable the AcceptsDeliveredExperience trait if
				// ValidStances includes Enemy actors.
				if (targetGainsExperience.Level == targetGainsExperience.MaxLevel)
					return false;

				return ade.ValidStances.HasStance(target.Owner.Stances[self.Owner])
					&& (ade.ValidTypes.Count == 0
						|| (!string.IsNullOrEmpty(type) && ade.ValidTypes.Contains(type)));
			}
		}
	}
}
