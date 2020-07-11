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
	[Desc("Add to a building to expose a move cursor that triggers Transforms and issues a move order to the transformed actor.")]
	public class TransformsIntoAircraftInfo : ConditionalTraitInfo, Requires<TransformsInfo>
	{
		[Desc("Can the actor be ordered to move in to shroud?")]
		public readonly bool MoveIntoShroud = true;

		[ActorReference]
		[FieldLoader.Require]
		public readonly HashSet<string> DockActors = new HashSet<string> { };

		[VoiceReference]
		public readonly string Voice = "Action";

		[Desc("Require the force-move modifier to display the move cursor.")]
		public readonly bool RequiresForceMove = false;

		[Desc("Cursor to display when able to land at target building.")]
		public readonly string EnterCursor = "enter";

		[Desc("Cursor to display when unable to land at target building.")]
		public readonly string EnterBlockedCursor = "enter-blocked";

		public override object Create(ActorInitializer init) { return new TransformsIntoAircraft(init, this); }
	}

	public class TransformsIntoAircraft : ConditionalTrait<TransformsIntoAircraftInfo>, IIssueOrder, IResolveOrder, IOrderVoice
	{
		readonly Actor self;
		Transforms[] transforms;

		public TransformsIntoAircraft(ActorInitializer init, TransformsIntoAircraftInfo info)
			: base(info)
		{
			self = init.Self;
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
				{
					yield return new EnterAlliedActorTargeter<BuildingInfo>(
						"Enter",
						5,
						Info.EnterCursor,
						Info.EnterBlockedCursor,
						AircraftCanEnter,
						target => Reservable.IsAvailableFor(target, self));

					yield return new AircraftMoveOrderTargeter(self, this);
				}
			}
		}

		public bool AircraftCanEnter(Actor a, TargetModifiers modifiers)
		{
			if (Info.RequiresForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove))
				return false;

			return AircraftCanEnter(a);
		}

		public bool AircraftCanEnter(Actor a)
		{
			return !self.AppearsHostileTo(a) && Info.DockActors.Contains(a.Info.Name);
		}

		// Note: Returns a valid order even if the unit can't move to the target
		Order IIssueOrder.IssueOrder(Actor self, IOrderTargeter order, Target target, bool queued)
		{
			if (order.OrderID == "Enter" || order.OrderID == "Move")
				return new Order(order.OrderID, self, target, queued);

			return null;
		}

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.OrderString == "Move")
			{
				var cell = self.World.Map.Clamp(self.World.Map.CellContaining(order.Target.CenterPosition));
				if (!Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
					return;

				var target = Target.FromCell(self.World, cell);
			}
			else if (order.OrderString == "Enter")
			{
				// Enter and Repair orders are only valid for own/allied actors,
				// which are guaranteed to never be frozen.
				if (order.Target.Type != TargetType.Actor)
					return;

				var targetActor = order.Target.Actor;
			}

			var currentTransform = self.CurrentActivity as Transform;
			var transform = transforms.FirstOrDefault(t => !t.IsTraitDisabled && !t.IsTraitPaused);
			if (transform == null && currentTransform == null)
				return;

			// Manually manage the inner activity queue
			var activity = currentTransform ?? transform.GetTransformActivity(self);
			if (!order.Queued && activity.NextActivity != null)
				activity.NextActivity.Cancel(self);

			activity.Queue(new IssueOrderAfterTransform(order.OrderString, order.Target, Color.Green));

			if (currentTransform == null)
				self.QueueActivity(order.Queued, activity);

			self.ShowTargetLines();
		}

		string IOrderVoice.VoicePhraseForOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return null;

			switch (order.OrderString)
			{
				case "Move":
					if (!Info.MoveIntoShroud && order.Target.Type != TargetType.Invalid)
					{
						var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
						if (!self.Owner.Shroud.IsExplored(cell))
							return null;
					}

					return Info.Voice;
				case "Enter":
					return Info.Voice;
				default: return null;
			}
		}

		class AircraftMoveOrderTargeter : IOrderTargeter
		{
			readonly TransformsIntoAircraft aircraft;

			public bool TargetOverridesSelection(Actor self, Target target, List<Actor> actorsAt, CPos xy, TargetModifiers modifiers)
			{
				// Always prioritise orders over selecting other peoples actors or own actors that are already selected
				if (target.Type == TargetType.Actor && (target.Actor.Owner != self.Owner || self.World.Selection.Contains(target.Actor)))
					return true;

				return modifiers.HasModifier(TargetModifiers.ForceMove);
			}

			public AircraftMoveOrderTargeter(Actor self, TransformsIntoAircraft aircraft)
			{
				this.aircraft = aircraft;
			}

			public string OrderID { get { return "Move"; } }
			public int OrderPriority { get { return 4; } }
			public bool IsQueued { get; protected set; }

			public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
			{
				if (target.Type != TargetType.Terrain || (aircraft.Info.RequiresForceMove && !modifiers.HasModifier(TargetModifiers.ForceMove)))
					return false;

				var location = self.World.Map.CellContaining(target.CenterPosition);
				var explored = self.Owner.Shroud.IsExplored(location);
				cursor = self.World.Map.Contains(location) ?
					self.World.Map.GetTerrainInfo(location).CustomCursor ?? "move" : "move-blocked";

				IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

				if (!(self.CurrentActivity is Transform || aircraft.transforms.Any(t => !t.IsTraitDisabled && !t.IsTraitPaused))
					|| (!explored && !aircraft.Info.MoveIntoShroud))
					cursor = "move-blocked";

				return true;
			}
		}
	}
}
