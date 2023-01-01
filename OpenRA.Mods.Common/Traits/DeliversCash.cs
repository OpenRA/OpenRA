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

using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Donate money to actors with the `" + nameof(AcceptsDeliveredCash) + "` trait.")]
	class DeliversCashInfo : TraitInfo
	{
		[Desc("The amount of cash the owner receives.")]
		public readonly int Payload = 500;

		[Desc("The amount of experience the donating player receives.")]
		public readonly int PlayerExperience = 0;

		[Desc("Identifier checked against AcceptsDeliveredCash.ValidTypes. Only needed if the latter is not empty.")]
		public readonly string Type = null;

		[Desc("Sound to play when delivering cash")]
		public readonly string[] Sounds = Array.Empty<string>();

		[CursorReference]
		[Desc("Cursor to display when hovering over a valid actor to deliver cash to.")]
		public readonly string Cursor = "enter";

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line.")]
		public readonly Color TargetLineColor = Color.Yellow;

		public override object Create(ActorInitializer init) { return new DeliversCash(this); }
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
			get { yield return new DeliversCashOrderTargeter(info); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID != "DeliverCash")
				return null;

			return new Order(order.OrderID, self, target, queued);
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeliverCash")
				return null;

			return info.Voice;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "DeliverCash")
				return;

			self.QueueActivity(order.Queued, new DonateCash(self, order.Target, info.Payload, info.PlayerExperience, info.TargetLineColor));
			self.ShowTargetLines();
		}

		void INotifyCashTransfer.OnAcceptingCash(Actor self, Actor donor) { }

		void INotifyCashTransfer.OnDeliveringCash(Actor self, Actor acceptor)
		{
			if (info.Sounds.Length > 0)
				Game.Sound.Play(SoundType.World, info.Sounds, self.World, self.CenterPosition);
		}

		public class DeliversCashOrderTargeter : UnitOrderTargeter
		{
			public DeliversCashOrderTargeter(DeliversCashInfo info)
				: base("DeliverCash", 5, info.Cursor, false, true) { }

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				var targetInfo = target.Info.TraitInfoOrDefault<AcceptsDeliveredCashInfo>();
				if (targetInfo == null || !targetInfo.ValidRelationships.HasRelationship(target.Owner.RelationshipWith(self.Owner)))
					return false;

				if (targetInfo.ValidTypes.Count == 0)
					return true;

				var type = self.Info.TraitInfo<DeliversCashInfo>().Type;
				return !string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type);
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				var targetInfo = target.Info.TraitInfoOrDefault<AcceptsDeliveredCashInfo>();
				if (targetInfo == null || !targetInfo.ValidRelationships.HasRelationship(target.Owner.RelationshipWith(self.Owner)))
					return false;

				if (targetInfo.ValidTypes.Count == 0)
					return true;

				var type = self.Info.TraitInfo<DeliversCashInfo>().Type;
				return !string.IsNullOrEmpty(type) && targetInfo.ValidTypes.Contains(type);
			}
		}
	}
}
