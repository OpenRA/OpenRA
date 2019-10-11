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
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class PickupUnit : Activity
	{
		readonly Actor cargo;
		readonly IMove movement;
		readonly Carryall carryall;
		readonly Carryable carryable;
		readonly int delay;

		bool locked;

		public PickupUnit(Actor self, Actor cargo, int delay)
		{
			this.cargo = cargo;
			this.delay = delay;
			carryable = cargo.Trait<Carryable>();
			movement = self.Trait<IMove>();
			carryall = self.Trait<Carryall>();

			ChildHasPriority = false;
		}

		protected override void OnFirstRun(Actor self)
		{
			carryall.ReserveCarryable(self, cargo);

			// Land at the target location
			QueueChild(new Land(self, Target.FromActor(cargo), carryall.OffsetForCarryable(self, cargo)));

			// Pause briefly before attachment for visual effect
			if (delay > 0)
				QueueChild(new Wait(delay, false));

			// Remove our carryable from world
			QueueChild(new AttachUnit(self, cargo));
			QueueChild(new TakeOff(self));
		}

		public override bool Tick(Actor self)
		{
			if (cargo != carryall.Carryable)
				return true;

			if (IsCanceling)
			{
				if (carryall.State == Carryall.CarryallState.Reserved)
					carryall.UnreserveCarryable(self);

				return true;
			}

			if (cargo.IsDead || carryable.IsTraitDisabled || !cargo.AppearsFriendlyTo(self))
			{
				carryall.UnreserveCarryable(self);
				return true;
			}

			var distance = (cargo.CenterPosition - self.CenterPosition).HorizontalLength;
			if (distance <= 4096 && !locked)
			{
				if (!carryable.LockForPickup(cargo, self))
					Cancel(self);

				locked = true;
			}

			return TickChild(self);
		}

		public override IEnumerable<TargetLineNode> TargetLineNodes(Actor self)
		{
			yield return new TargetLineNode(Target.FromActor(cargo), Color.Yellow);
		}

		class AttachUnit : Activity
		{
			readonly Actor cargo;
			readonly Carryable carryable;
			readonly Carryall carryall;

			public AttachUnit(Actor self, Actor cargo)
			{
				this.cargo = cargo;
				carryable = cargo.Trait<Carryable>();
				carryall = self.Trait<Carryall>();
			}

			protected override void OnFirstRun(Actor self)
			{
				// The cargo might have become invalid while we were moving towards it.
				if (cargo == null || cargo.IsDead || carryable.IsTraitDisabled || !cargo.AppearsFriendlyTo(self))
					return;

				self.World.AddFrameEndTask(w =>
				{
					cargo.World.Remove(cargo);
					carryable.Attached(cargo);
					carryall.AttachCarryable(self, cargo);
				});
			}
		}
	}
}
