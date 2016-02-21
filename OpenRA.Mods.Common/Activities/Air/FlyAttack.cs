#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class FlyAttack : Activity
	{
		readonly Target target;
		readonly AttackPlane attackPlane;
		readonly AmmoPool[] ammoPools;

		Activity inner;
		int ticksUntilTurn;

		public FlyAttack(Actor self, Target target)
		{
			this.target = target;
			attackPlane = self.TraitOrDefault<AttackPlane>();
			ammoPools = self.TraitsImplementing<AmmoPool>().ToArray();
			ticksUntilTurn = attackPlane.AttackPlaneInfo.AttackTurnDelay;
		}

		public override Activity Tick(Actor self)
		{
			if (!target.IsValidFor(self))
				return NextActivity;

			// Move to the next activity only if all ammo pools are depleted and none reload automatically
			// TODO: This should check whether there is ammo left that is actually suitable for the target
			if (ammoPools.All(x => !x.Info.SelfReloads && !x.HasAmmo()))
				return NextActivity;

			if (attackPlane != null)
				attackPlane.DoAttack(self, target);

			if (inner == null)
			{
				if (IsCanceled)
					return NextActivity;

				// TODO: This should fire each weapon at its maximum range
				if (attackPlane != null && target.IsInRange(self.CenterPosition, attackPlane.Armaments.Select(a => a.Weapon.MinRange).Min()))
					inner = ActivityUtils.SequenceActivities(new FlyTimed(ticksUntilTurn, self), new Fly(self, target), new FlyTimed(ticksUntilTurn, self));
				else
					inner = ActivityUtils.SequenceActivities(new Fly(self, target), new FlyTimed(ticksUntilTurn, self));
			}

			inner = ActivityUtils.RunActivity(self, inner);

			return this;
		}

		public override void Cancel(Actor self)
		{
			if (!IsCanceled && inner != null)
				inner.Cancel(self);

			// NextActivity must always be set to null:
			base.Cancel(self);
		}
	}
}
