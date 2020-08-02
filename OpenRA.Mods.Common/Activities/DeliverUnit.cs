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

using System.Collections.Generic;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class DeliverUnit : Activity
	{
		readonly Carryall carryall;
		readonly BodyOrientation body;
		readonly bool assignTargetOnFirstRun;
		readonly WDist deliverRange;

		Target destination;

		public DeliverUnit(Actor self, WDist deliverRange)
			: this(self, Target.Invalid, deliverRange)
		{
			assignTargetOnFirstRun = true;
		}

		public DeliverUnit(Actor self, in Target destination, WDist deliverRange)
		{
			this.destination = destination;
			this.deliverRange = deliverRange;

			carryall = self.Trait<Carryall>();
			body = self.Trait<BodyOrientation>();
		}

		protected override void OnFirstRun(Actor self)
		{
			// In case this activity was queued (either via queued order of via AutoCarryall)
			// something might have happened to the cargo in the time between the activity being
			// queued and being run, so short out if it is no longer valid.
			if (carryall.Carryable == null)
				return;

			if (assignTargetOnFirstRun)
				destination = Target.FromCell(self.World, self.Location);

			QueueChild(new Land(self, destination, deliverRange));
			QueueChild(new Wait(carryall.Info.BeforeUnloadDelay, false));
			QueueChild(new ReleaseUnit(self));
			QueueChild(new TakeOff(self));
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(destination, carryall.Info.TargetLineColor);
		}

		class ReleaseUnit : Activity
		{
			readonly Carryall carryall;
			readonly BodyOrientation body;
			readonly IFacing facing;

			public ReleaseUnit(Actor self)
			{
				facing = self.Trait<IFacing>();
				carryall = self.Trait<Carryall>();
				body = self.Trait<BodyOrientation>();
			}

			protected override void OnFirstRun(Actor self)
			{
				self.Trait<Aircraft>().RemoveInfluence();

				var localOffset = carryall.CarryableOffset.Rotate(body.QuantizeOrientation(self, self.Orientation));
				var targetPosition = self.CenterPosition + body.LocalToWorld(localOffset);
				var targetLocation = self.World.Map.CellContaining(targetPosition);
				carryall.Carryable.Trait<IPositionable>().SetPosition(carryall.Carryable, targetLocation, SubCell.FullCell);
				carryall.Carryable.Trait<IFacing>().Facing = facing.Facing;

				// Put back into world
				self.World.AddFrameEndTask(w =>
				{
					if (self.IsDead)
						return;

					var cargo = carryall.Carryable;
					if (cargo == null)
						return;

					var carryable = cargo.Trait<Carryable>();
					w.Add(cargo);
					carryall.DetachCarryable(self);
					carryable.UnReserve(cargo);
					carryable.Detached(cargo);
				});
			}
		}
	}
}
