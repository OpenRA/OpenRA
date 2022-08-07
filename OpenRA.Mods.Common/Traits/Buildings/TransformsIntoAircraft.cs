#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Add to a building to expose a move cursor that triggers Transforms and issues a move order to the transformed actor.")]
	public class TransformsIntoAircraftInfo : ConditionalTraitInfo, Requires<TransformsInfo>
	{
		[Desc("Can the actor be ordered to move in to shroud?")]
		public readonly bool MoveIntoShroud = true;

		[Desc("Color to use for the target line for regular move orders.")]
		public readonly Color TargetLineColor = Color.Green;

		public override object Create(ActorInitializer init) { return new TransformsIntoAircraft(init, this); }
	}

	public class TransformsIntoAircraft : ConditionalTrait<TransformsIntoAircraftInfo>, IResolveOrder
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

		void IResolveOrder.ResolveOrder(Actor self, Order order)
		{
			if (IsTraitDisabled)
				return;

			if (order.OrderString == "Move")
			{
				var cell = self.World.Map.Clamp(self.World.Map.CellContaining(order.Target.CenterPosition));
				if (!Info.MoveIntoShroud && !self.Owner.Shroud.IsExplored(cell))
					return;
			}
			else
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
	}
}
