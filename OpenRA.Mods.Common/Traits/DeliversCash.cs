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
	[Desc("Donate money to actors with the `AcceptsDeliveredCash` trait.")]
	class DeliversCashInfo : ConditionalTraitInfo
	{
		[Desc("The amount of cash the owner receives.")]
		public readonly int Payload = 500;

		[Desc("The amount of experience the donating player receives.")]
		public readonly int PlayerExperience = 0;

		[Desc("Identifier checked against AcceptsDeliveredCash.ValidTypes. Only needed if the latter is not empty.")]
		public readonly string Type = null;

		[Desc("Sound to play when delivering cash")]
		public readonly string[] Sounds = { };

		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new DeliversCash(this); }
	}

	class DeliversCash : ConditionalTrait<DeliversCashInfo>, IIssueOrder, IResolveOrder, IOrderVoice, INotifyCashTransfer
	{
		public DeliversCash(DeliversCashInfo info)
			: base(info) { }

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				if (IsTraitDisabled)
					yield break;

				yield return new DeliversCashOrderTargeter(Info);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "DeliverCash")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeliverCash")
				return null;

			return Info.Voice;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeliverCash")
				return;

			self.QueueActivity(order.Queued, new DonateCash(self, order.Target, this));
			self.ShowTargetLines();
		}

		void INotifyCashTransfer.OnAcceptingCash(Actor self, Actor donor) { }

		void INotifyCashTransfer.OnDeliveringCash(Actor self, Actor acceptor)
		{
			if (Info.Sounds.Length > 0)
				Game.Sound.Play(SoundType.World, Info.Sounds, self.World, self.CenterPosition);
		}

		public class DeliversCashOrderTargeter : UnitOrderTargeter
		{
			readonly DeliversCashInfo info;

			public DeliversCashOrderTargeter(DeliversCashInfo info)
				: base("DeliverCash", 5, "enter", false, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var adc = target.TraitOrDefault<AcceptsDeliveredCash>();
				if (adc == null)
					return false;

				if (adc.IsTraitDisabled)
					return false;

				var type = info.Type;
				return adc.Info.ValidStances.HasStance(target.Owner.Stances[self.Owner])
					&& (adc.Info.ValidTypes.Count == 0
						|| (!string.IsNullOrEmpty(type) && adc.Info.ValidTypes.Contains(type)));
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var adc = target.Info.TraitInfoOrDefault<AcceptsDeliveredCashInfo>();
				if (adc == null)
					return false;

				// TODO: FrozenActors don't yet have a way of caching conditions, so we can't filter disabled traits
				// This therefore assumes that all AcceptsDeliveredCash traits are enabled, which is probably wrong.
				// Actors with FrozenUnderFog should therefore not disable the AcceptsDeliveredCash trait if
				// ValidStances includes Enemy actors.
				var type = info.Type;
				return adc.ValidStances.HasStance(target.Owner.Stances[self.Owner])
					&& (adc.ValidTypes.Count == 0
						|| (!string.IsNullOrEmpty(type) && adc.ValidTypes.Contains(type)));
			}
		}
	}
}
