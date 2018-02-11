#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Can move actors instantly to primary designated teleport network canal actor.")]
	class TeleportNetworkTransportableInfo : ITraitInfo
	{
		[VoiceReference]
		public readonly string Voice = "Action";
		public readonly string EnterCursor = "enter";
		public readonly string EnterBlockedCursor = "enter-blocked";
		public object Create(ActorInitializer init) { return new TeleportNetworkTransportable(init, this); }
	}

	class TeleportNetworkTransportable : IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly TeleportNetworkTransportableInfo info;

		public TeleportNetworkTransportable(ActorInitializer init, TeleportNetworkTransportableInfo info)
		{
			this.info = info;
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get { yield return new TeleportNetworkTransportOrderTargeter(info); }
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID != "TeleportNetworkTransport")
				return null;

			return new Order(order.OrderID, self, target, queued) { };
		}

		// Checks if targeted actor's owner has enough canals (more than 1) of provided type
		static bool HasEnoughCanals(Actor targetactor, string type)
		{
			var counter = targetactor.Owner.PlayerActor.TraitsImplementing<TeleportNetworkManager>().Where(x => x.Type == type).First();

			if (counter == null)
				return false;

			return counter.Count > 1;
		}

		static bool IsValidOrder(Actor self, Order order)
		{
			// Not targeting a frozen actor
			if (order.ExtraData == 0 && order.TargetActor == null)
				return false;

			var teleporttrait = order.TargetActor.TraitOrDefault<TeleportNetwork>();

			if (teleporttrait == null)
				return false;

			if (!HasEnoughCanals(order.TargetActor, teleporttrait.Info.Type))
				return false;

			return !order.TargetActor.IsPrimaryTeleportNetworkExit();
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "TeleportNetworkTransport" && IsValidOrder(self, order)
				? info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != "TeleportNetworkTransport" || !IsValidOrder(self, order))
				return;

			var target = self.ResolveFrozenActorOrder(order, Color.Yellow);
			if (target.Type != TargetType.Actor)
				return;

			var targettrait = order.TargetActor.TraitOrDefault<TeleportNetwork>();

			if (targettrait == null)
				return;

			if (!order.Queued)
				self.CancelActivity();

			self.SetTargetLine(target, Color.Yellow);
			self.QueueActivity(new EnterTeleportNetwork(self, target.Actor, EnterBehaviour.Exit, targettrait.Info.Type));
		}

		class TeleportNetworkTransportOrderTargeter : UnitOrderTargeter
		{
			TeleportNetworkTransportableInfo info;

			public TeleportNetworkTransportOrderTargeter(TeleportNetworkTransportableInfo info)
				: base("TeleportNetworkTransport", 6, info.EnterCursor, true, true)
			{
				this.info = info;
			}

			public override bool CanTargetActor(Actor self, Actor target, TargetModifiers modifiers, ref string cursor)
			{
				if (modifiers.HasFlag(TargetModifiers.ForceAttack))
					return false;

				if (self.Owner.Stances[target.Owner] == Stance.Enemy && !modifiers.HasFlag(TargetModifiers.ForceMove))
					return false;

				var trait = target.TraitOrDefault<TeleportNetwork>();
				if (trait == null)
					return false;

				if (!target.IsValidTeleportNetworkUser(self)) // block, if primary exit.
					cursor = info.EnterBlockedCursor;

				return true;
			}

			public override bool CanTargetFrozenActor(Actor self, FrozenActor target, TargetModifiers modifiers, ref string cursor)
			{
				// You can't enter frozen actor.
				return false;
			}
		}
	}
}
