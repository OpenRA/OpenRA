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

/*
// Modifications for Mod.yupgi_alert:
// Add sound effect parameter and INotifyCashTransfer.
*/

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Donate money to actors with the `AcceptsDeliveredCash` trait.")]
	class DeliversCashInfo : ITraitInfo
	{
		[Desc("The amount of cash the owner receives.")]
		public readonly int Payload = 500;

		[Desc("The amount of experience the donating player receives.")]
		public readonly int PlayerExperience = 0;

		[Desc("Identifier checked against AcceptsDeliveredCash.ValidTypes. Only needed if the latter is not empty.")]
		public readonly string Type = null;

		[Desc("Sound to play when delivering cash")]
		public readonly string[] Sounds = { };

		[VoiceReference] public readonly string Voice = "Action";

		public object Create(ActorInitializer init) { return new DeliversCash(this); }
	}

	class DeliversCash : IIssueOrder, IResolveOrder, IOrderVoice, INotifyCashTransfer
	{
		readonly DeliversCashInfo info;

		public DeliversCash(DeliversCashInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new DeliversCashOrderTargeter(); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "DeliverCash")
				return null;

			if (target.Type == TargetType.FrozenActor)
				return new Order(order.OrderID, self, queued) { ExtraData = target.FrozenActor.ID };

			return new Order(order.OrderID, self, queued) { TargetActor = target.Actor };
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return info.Voice;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeliverCash")
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
			if (target.Type != TargetType.Actor)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Yellow);
			self.QueueActivity(new DonateCash(self, target.Actor, info.Payload, info.PlayerExperience));
		}

		void INotifyCashTransfer.OnCashTransfer(Actor self, Actor donor)
		{
			if (info.Sounds.Length > 0)
				Game.Sound.Play(SoundType.World, info.Sounds.Random(self.World.SharedRandom), self.CenterPosition);
		}

		public class DeliversCashOrderTargeter : UnitOrderTargeter
		{
			public DeliversCashOrderTargeter()
				: base("DeliverCash", 5, "enter", false, true) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var type = self.Info.TraitInfo<DeliversCashInfo>().Type;
				var targetInfo = target.Info.TraitInfoOrDefault<AcceptsDeliveredCashInfo>();
				return targetInfo != null
					&& targetInfo.ValidStances.HasStance(target.Owner.Stances[self.Owner])
					&& (targetInfo.ValidTypes.Count == 0
						|| (!string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type)));
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var type = self.Info.TraitInfo<DeliversCashInfo>().Type;
				var targetInfo = target.Info.TraitInfoOrDefault<AcceptsDeliveredCashInfo>();
				return targetInfo != null
					&& targetInfo.ValidStances.HasStance(target.Owner.Stances[self.Owner])
					&& (targetInfo.ValidTypes.Count == 0
						|| (!string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type)));
			}
		}
	}
}
