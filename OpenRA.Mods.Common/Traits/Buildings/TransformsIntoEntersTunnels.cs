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
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Add to a building to expose a move cursor that triggers Transforms and issues an enter tunnel order to the transformed actor.")]
	public class TransformsIntoEntersTunnelsInfo : ConditionalTraitInfo, Requires<TransformsInfo>
	{
		[CursorReference]
		[Desc("Cursor to display when able to enter target tunnel.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to enter target tunnel.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		[Desc("Color to use for the target line while in tunnels.")]
		public readonly Color TargetLineColor = Color.Green;

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Require the force-move modifier to display the enter cursor.")]
		public readonly bool RequiresForceMove = false;

		public override object Create(ActorInitializer init) { return new TransformsIntoEntersTunnels(init.Self, this); }
	}

	public class TransformsIntoEntersTunnels : ConditionalTrait<TransformsIntoEntersTunnelsInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		Transforms[] transforms;

		public TransformsIntoEntersTunnels(Actor self, TransformsIntoEntersTunnelsInfo info)
			: base(info)
		{
			this.self = self;
		}

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
					yield return new EntersTunnels.EnterTunnelOrderTargeter(Info.EnterCursor, Info.EnterBlockedCursor, CanEnterTunnel, UseEnterCursor);
			}
		}

		bool CanEnterTunnel(Actor target, TargetModifiers modifiers)
		{
			return !Info.RequiresForceMove || modifiers.HasModifier(TargetModifiers.ForceMove);
		}

		bool UseEnterCursor(Actor target)
		{
			return self.CurrentActivity is Transform || transforms.Any(t => !t.IsTraitDisabled && !t.IsTraitPaused);
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
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

			// Manually manage the inner activity queue
			var activity = currentTransform ?? transform.GetTransformActivity();
			if (!order.Queued)
				activity.NextActivity?.Cancel(self);

			activity.Queue(new IssueOrderAfterTransform(order.OrderString, order.Target, Info.TargetLineColor));

			if (currentTransform == null)
				self.QueueActivity(order.Queued, activity);

			self.ShowTargetLines();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "EnterTunnel" ? Info.Voice : null;
		}
	}
}
