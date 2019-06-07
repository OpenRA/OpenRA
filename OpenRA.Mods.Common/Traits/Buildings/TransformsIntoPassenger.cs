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
	[Desc("Add to a building to expose a move cursor that triggers Transforms and issues an EnterTransport order to the transformed actor.")]
	public class TransformsIntoPassengerInfo : ConditionalTraitInfo, Requires<TransformsInfo>
	{
		public readonly string CargoType = null;
		public readonly int Weight = 1;

		[VoiceReference]
		public readonly string Voice = "Action";

		public override object Create(ActorInitializer init) { return new TransformsIntoPassenger(this); }
	}

	public class TransformsIntoPassenger : ConditionalTrait<TransformsIntoPassengerInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		Transforms[] transforms;

		public TransformsIntoPassenger(TransformsIntoPassengerInfo info)
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
					yield return new EnterAlliedActorTargeter<CargoInfo>("EnterTransport", 5, IsCorrectCargoType, CanEnter);
			}
		}

		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "EnterTransport")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool IsCorrectCargoType(Actor target)
		{
			var ci = target.Info.TraitInfo<CargoInfo>();
			return ci.Types.Contains(Info.CargoType);
		}

		bool CanEnter(Actor target)
		{
			if (!transforms.Any(t => !t.IsTraitDisabled && !t.IsTraitPaused))
				return false;

			var cargo = target.TraitOrDefault<Cargo>();
			return cargo != null && cargo.HasSpace(Info.Weight);
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.OrderString != "EnterTransport")
				return;

			// Enter orders are only valid for own/allied actors,
			// which are guaranteed to never be frozen.
			if (order.Target.Type != TargetType.Actor)
				return;

			var targetActor = order.Target.Actor;
			if (!CanEnter(targetActor))
				return;

			if (!IsCorrectCargoType(targetActor))
				return;

			var currentTransform = self.CurrentActivity as Transform;
			var transform = transforms.FirstOrDefault(t => !t.IsTraitDisabled && !t.IsTraitPaused);
			if (transform == null && currentTransform == null)
				return;

			self.SetTargetLine(order.Target, Color.Green);

			var activity = currentTransform ?? transform.GetTransformActivity(self);
			activity.Queue(self, new IssueOrderAfterTransform(order.OrderString, order.Target));

			if (currentTransform == null)
				self.QueueActivity(order.Queued, activity);
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return null;

			if (order.OrderString != "EnterTransport")
				return null;

			if (order.Target.Type != TargetType.Actor || !CanEnter(order.Target.Actor))
				return null;

			return Info.Voice;
		}
	}
}
