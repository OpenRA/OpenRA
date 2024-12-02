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
	[Desc("Add to a building to expose a move cursor that triggers Transforms and issues a dock order to the transformed actor.")]
	public class TransformsIntoDockClientInfo : ConditionalTraitInfo, Requires<TransformsInfo>, IDockClientManagerInfo
	{
		[CursorReference]
		[Desc("Cursor to display when able to dock at target actor.")]
		public readonly string EnterCursor = "enter";

		[CursorReference]
		[Desc("Cursor to display when unable to dock at target actor.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		[VoiceReference]
		[Desc("Voice.")]
		public readonly string Voice = "Action";

		[Desc("Color to use for the target line of docking orders.")]
		public readonly Color DockLineColor = Color.Green;

		[Desc("Require the force-move modifier to display the dock cursor.")]
		public readonly bool RequiresForceMove = false;

		public override object Create(ActorInitializer init) { return new TransformsIntoDockClient(init.Self, this); }
	}

	public class TransformsIntoDockClient : ConditionalTrait<TransformsIntoDockClientInfo>, IResolveOrder, IOrderVoice, IIssueOrder
	{
		readonly Actor self;
		protected IDockClient[] dockClients;

		readonly Transforms[] transforms;

		public TransformsIntoDockClient(Actor self, TransformsIntoDockClientInfo info)
			: base(info)
		{
			this.self = self;
			transforms = self.TraitsImplementing<Transforms>().ToArray();
		}

		protected override void Created(Actor self)
		{
			base.Created(self);
			dockClients = self.TraitsImplementing<IDockClient>().ToArray();
		}

		IEnumerable<IOrderTargeter> IIssueOrder.Orders
		{
			get
			{
				yield return new DockActorTargeter(
					6,
					Info.EnterCursor,
					Info.EnterBlockedCursor,
					() => Info.RequiresForceMove,
					CanQueueDockAt,
					CanDockAt);
			}
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Dock" || order.OrderString == "ForceDock")
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

				activity.Queue(new IssueOrderAfterTransform(order.OrderString, order.Target, Info.DockLineColor));

				if (currentTransform == null)
					self.QueueActivity(order.Queued, activity);

				self.ShowTargetLines();
			}
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (order.Target.Type != TargetType.Actor || IsTraitDisabled)
				return null;

			if (order.OrderString != "Dock" && order.OrderString != "ForceDock")
				return null;

			if (CanQueueDockAt(order.Target.Actor, order.OrderString == "ForceDock", order.Queued))
				return Info.Voice;

			return null;
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, in Target target, bool queued)
		{
			if (order.OrderID == "Dock" || order.OrderID == "ForceDock")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		/// <summary>Clone of <see cref="DockClientManager.CanDockAt(Actor, bool, bool)"/>.</summary>
		public bool CanDockAt(Actor target, bool forceEnter)
		{
			if (!(self.CurrentActivity is Transform || transforms.Any(t => !t.IsTraitDisabled && !t.IsTraitPaused)))
				return false;

			return !IsTraitDisabled && target.TraitsImplementing<DockHost>().Any(
				host => dockClients.Any(client => client.CanDockAt(target, host, forceEnter, true)));
		}

		/// <summary>Clone of <see cref="DockClientManager.CanQueueDockAt(Actor, bool, bool)"/>.</summary>
		public bool CanQueueDockAt(Actor target, bool forceEnter, bool isQueued)
		{
			if (Info.RequiresForceMove && !forceEnter)
				return false;

			return (!IsTraitDisabled)
				&& target.TraitsImplementing<IDockHost>().Any(
					host => dockClients.Any(client => client.CanQueueDockAt(target, host, forceEnter, isQueued)));
		}
	}
}
