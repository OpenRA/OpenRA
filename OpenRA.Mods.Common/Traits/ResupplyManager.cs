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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Orders;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("This actor can be sent to a structure for resupply.")]
	public class ResupplyManagerInfo : ITraitInfo, Requires<IMoveInfo>
	{
		[VoiceReference] public readonly string Voice = "Action";

		public virtual object Create(ActorInitializer init) { return new ResupplyManager(init.Self, this); }
	}

	public class ResupplyManager : IIssueOrder, IResolveOrder, IOrderVoice, INotifyCreated
	{
		public readonly ResupplyManagerInfo Info;
		readonly IMove movement;
		IResupplyable[] resupplyables;

		public ResupplyManager(Actor self, ResupplyManagerInfo info)
		{
			Info = info;
			movement = self.Trait<IMove>();
		}

		void INotifyCreated.Created(Actor self)
		{
			resupplyables = self.TraitsImplementing<IResupplyable>().ToArray();
		}

		public IEnumerable<IOrderTargeter> Orders
		{
			get
			{
				yield return new EnterAlliedActorTargeter<BuildingInfo>("Resupply", 5, CanResupplyAt, NeedsResupplyAt);
			}
		}

		public Order IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Resupply")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		bool CanResupplyAt(Actor target)
		{
			foreach (var r in resupplyables)
				if (r.CanResupplyAt(target))
					return true;

			return false;
		}

		bool NeedsResupplyAt(Actor target)
		{
			foreach (var r in resupplyables)
				if (r.NeedsResupplyAt(target))
					return true;

			return false;
		}

		public string VoicePhraseForOrder(Actor self, Order order)
		{
			return order.OrderString == "Resupply" ? Info.Voice : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString == "Resupply")
			{
				// Resupply orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				if (order.Target.Type != TargetType.Actor)
					return;

				// Aircraft handle Resupply orders directly in the Aircraft trait
				if (self.Info.HasTraitInfo<AircraftInfo>())
					return;

				if (!order.Queued)
					self.CancelActivity();

				self.SetTargetLine(order.Target, Color.Green);
				var activities = ActivityUtils.SequenceActivities(self,
					movement.MoveToTarget(self, order.Target, targetLineColor: Color.Green),
					new CallFunc(() => AfterReachActivities(self, order, movement)));

				self.QueueActivity(new WaitForTransport(self, activities));
				TryCallTransport(self, order.Target, new CallFunc(() => AfterReachActivities(self, order, movement)));
			}
		}

		void AfterReachActivities(Actor self, Order order, IMove movement)
		{
			if (order.Target.Type != TargetType.Actor)
				return;

			var targetActor = order.Target.Actor;
			if (!targetActor.IsInWorld || targetActor.IsDead || !targetActor.TraitsImplementing<IResupplier>().Any(Exts.IsTraitEnabled))
				return;

			if (!NeedsResupplyAt(order.Target.Actor))
				return;

			// Return if we no longer need resupplies (for example if we were resupplied through other means since the order was given)
			var validResupplyables = resupplyables.Where(r => r.NeedsResupplyAt(targetActor));
			if (!validResupplyables.Any())
				return;

			var closeEnough = validResupplyables.OrderByDescending(c => c.CloseEnough).First().CloseEnough;

			// If closeEnough is not 512, this (currently) means it has to be RepairableNear.
			// TODO: This is hacky, but will be rewritten soon anyway, so this is OK for now.
			if (closeEnough.Length != 512)
				self.QueueActivity(movement.MoveWithinRange(order.Target, closeEnough, targetLineColor: Color.Green));
			else
				self.QueueActivity(movement.MoveTo(self.World.Map.CellContaining(targetActor.CenterPosition), targetActor));

			// Add a CloseEnough range to ensure we're at or close enough to the host actor
			self.QueueActivity(new Resupply(self, targetActor, closeEnough));

			var rp = targetActor.TraitOrDefault<RallyPoint>();
			if (rp != null)
			{
				self.QueueActivity(new CallFunc(() =>
				{
					self.SetTargetLine(Target.FromCell(self.World, rp.Location), Color.Green);
					self.QueueActivity(movement.MoveTo(rp.Location, targetActor));
				}));
			}
		}

		public Actor FindResupplyActor(Actor self)
		{
			var resupplyActor = self.World.ActorsWithTrait<IResupplier>()
				.Where(a => !a.Actor.IsDead && a.Actor.IsInWorld
					&& a.Actor.Owner.IsAlliedWith(self.Owner) &&
					resupplyables.Any(r => r.NeedsResupplyAt(a.Actor)))
				.OrderBy(p => (self.Location - p.Actor.Location).LengthSquared);

			// Worst case FirstOrDefault() will return a TraitPair<null, null>, which is OK.
			return resupplyActor.FirstOrDefault().Actor;
		}

		static void TryCallTransport(Actor self, Target target, Activity nextActivity)
		{
			var targetCell = self.World.Map.CellContaining(target.CenterPosition);
			var delta = (self.CenterPosition - target.CenterPosition).LengthSquared;
			var transports = self.TraitsImplementing<ICallForTransport>()
				.Where(t => t.MinimumDistance.LengthSquared < delta);

			foreach (var t in transports)
				t.RequestTransport(self, targetCell, nextActivity);
		}
	}
}
