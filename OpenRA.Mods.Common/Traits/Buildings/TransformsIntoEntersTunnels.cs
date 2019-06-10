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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Add to a building to expose a move cursor that triggers Transforms and issues an enter tunnel order to the transformed actor.")]
	public class TransformsIntoEntersTunnelsInfo : ConditionalTraitInfo, Requires<TransformsInfo>
	{
		public readonly string EnterCursor = "enter";
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Require the force-move modifier to display the enter cursor.")]
		public readonly bool RequiresForceMove = false;

		public override object Create(ActorInitializer init) { return new TransformsIntoEntersTunnels(this); }
	}

	public class TransformsIntoEntersTunnels : ConditionalTrait<TransformsIntoEntersTunnelsInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		Transforms[] transforms;

		public TransformsIntoEntersTunnels(TransformsIntoEntersTunnelsInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			transforms = self.TraitsImplementing<Transforms>().ToArray();
			base.Created(self);
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				if (!IsTraitDisabled)
					yield return new EntersTunnels.EnterTunnelOrderTargeter(Info.EnterCursor, Info.EnterBlockedCursor, () => Info.RequiresForceMove);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "EnterTunnel")
				return new Order(order.OrderID, self, target, queued) { SuppressVisualFeedback = true };

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled || order.OrderString != "EnterTunnel" || order.Target.Type != TargetType.Actor)
				return;

			var tunnel = order.Target.Actor.TraitOrDefault<TunnelEntrance>();
			if (tunnel == null || !tunnel.Exit.HasValue)
				return;

			var currentTransform = self.CurrentActivity as Transform;
			var transform = transforms.FirstOrDefault(t => !t.IsTraitDisabled && !t.IsTraitPaused);
			if (transform == null && currentTransform == null)
				return;

			self.SetTargetLine(order.Target, Color.Green);

			// Manually manage the inner activity queue
			var activity = currentTransform ?? transform.GetTransformActivity(self);
			if (!order.Queued && activity.NextActivity != null)
				activity.NextActivity.Cancel(self);

			activity.Queue(self, new IssueOrderAfterTransform(order.OrderString, order.Target));

			if (currentTransform == null)
				self.QueueActivity(order.Queued, activity);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EnterTunnel" ? Info.Voice : null;
		}
	}
}
