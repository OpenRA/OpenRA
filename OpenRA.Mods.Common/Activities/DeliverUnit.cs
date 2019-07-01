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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
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

		public DeliverUnit(Actor self, Target destination, WDist deliverRange)
		{
			this.destination = destination;
			this.deliverRange = deliverRange;

			carryall = self.Trait<Carryall>();
			body = self.Trait<BodyOrientation>();
		}

		protected override void OnFirstRun(Actor self)
		{
			if (assignTargetOnFirstRun)
				destination = Target.FromCell(self.World, self.Location);

			QueueChild(new Land(self, destination, deliverRange));
			QueueChild(new Wait(carryall.Info.BeforeUnloadDelay, false));
			QueueChild(new ReleaseUnit(self));
			QueueChild(new TakeOff(self));
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

				// HACK: directly manipulate the turret facings to match the new orientation
				// This can eventually go away, when we make turret facings relative to the body
				var carryableFacing = carryall.Carryable.Trait<IFacing>();
				var facingDelta = facing.Facing - carryableFacing.Facing;
				foreach (var t in carryall.Carryable.TraitsImplementing<Turreted>())
					t.TurretFacing += facingDelta;

				carryableFacing.Facing = facing.Facing;

				// Put back into world
				self.World.AddFrameEndTask(w =>
				{
					var cargo = carryall.Carryable;
					var carryable = carryall.Carryable.Trait<Carryable>();
					w.Add(cargo);
					carryall.DetachCarryable(self);
					carryable.UnReserve(cargo);
					carryable.Detached(cargo);
				});
			}
		}
	}
}
