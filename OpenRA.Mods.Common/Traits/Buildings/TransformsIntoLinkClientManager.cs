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
	[Desc("Add to a building to expose a move cursor that triggers Transforms and issues a link order to the transformed actor.")]
	public class TransformsIntoLinkClientManagerInfo : ConditionalTraitInfo, Requires<TransformsInfo>, ILinkClientManagerInfo
	{
		[CursorReference]
		[Desc("Cursor to display when able to link to target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to link to target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference]
		[Desc("Voice.")]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line of linking orders.")]
		public readonly Color LinkLineColor = Color.Green;

		[Desc("Require the force-move modifier to display the link cursor.")]
		public readonly bool RequiresForceMove = false;

		public override object Create(ActorInitializer init) { return new TransformsIntoLinkClientManager(init.Self, this); }
	}

	public class TransformsIntoLinkClientManager : ConditionalTrait<TransformsIntoLinkClientManagerInfo>, IResolveOrder, IOrderVoice, IIssueOrder
	{
		readonly Actor self;
		protected ILinkClient[] linkClients;

		readonly Transforms[] transforms;

		public TransformsIntoLinkClientManager(Actor self, TransformsIntoLinkClientManagerInfo info)
			: base(info)
		{
			this.self = self;
			transforms = self.TraitsImplementing<Transforms>().ToArray();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			linkClients = self.TraitsImplementing<ILinkClient>().ToArray();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new LinkActorTargeter(
					6,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					() => Info.RequiresForceMove,
					LinkingPossible,
					CanLinkTo);
			}
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Link" || order.OrderString == "ForceLink")
			{
				// Deliver orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				// TODO: support frozen actors.
				if (order.Target.Type != TargetType.Actor)
					return;

				if (IsTraitDisabled)
					return;

				var currentTransform = self.CurrentActivity as Transform;
				var transform = transforms.FirstOrDefault(t => !t.IsTraitDisabled && !t.IsTraitPaused);
				if (transform == null && currentTransform == null)
					return;

				// Manually manage the inner activity queue.
				var activity = currentTransform ?? transform.GetTransformActivity();
				if (!order.Queued)
					activity.NextActivity?.Cancel(self);

				activity.Queue(new IssueOrderAfterTransform(order.OrderString, order.Target, Info.LinkLineColor));

				if (currentTransform == null)
					self.QueueActivity(order.Queued, activity);

				self.ShowTargetLines();
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.OrderString == "Link" && CanLinkTo(order.Target.Actor, false))
				return Info.Voice;
			else if (order.OrderString == "ForceLink" && CanLinkTo(order.Target.Actor, true))
				return Info.Voice;

			return null;
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Link" || order.OrderID == "ForceLink")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		/// <summary>Clone of <see cref="LinkClientManager.LinkingPossible(Actor, bool)"/>.</summary>
		public bool LinkingPossible(Actor target, bool forceEnter)
		{
			return !IsTraitDisabled && target.TraitsImplementing<ILinkHost>().Any(host => linkClients.Any(client => client.IsLinkingPossible(host.GetLinkType)));
		}

		/// <summary>Clone of <see cref="LinkClientManager.CanLinkTo(Actor, bool, bool)"/>.</summary>
		public bool CanLinkTo(Actor target, bool forceEnter)
		{
			if (!(self.CurrentActivity is Transform || transforms.Any(t => !t.IsTraitDisabled && !t.IsTraitPaused)))
				return false;

			return !IsTraitDisabled && target.TraitsImplementing<ILinkHost>().Any(host => linkClients.Any(client => client.CanLinkTo(target, host, forceEnter, true)));
		}
	}
}
