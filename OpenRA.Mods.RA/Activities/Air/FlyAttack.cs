#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Traits;
using OpenRA.Traits;
using System.Linq;

namespace OpenRA.Mods.RA.Activities
{
	public class FlyAttack : Activity
	{
		readonly Target target;
		Activity inner;
		int ticksUntilTurn = 50;

		public FlyAttack(Target target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (!target.IsValidFor(self))
				return NextActivity;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return NextActivity;

			var attack = self.TraitOrDefault<AttackPlane>();
			if (attack != null)
				attack.DoAttack(self, target);

			if (inner == null)
			{
				if (IsCanceled)
					return NextActivity;

				if (target.IsInRange(self.CenterPosition, attack.Armaments.Select(a => a.Weapon.MinRange).Min()))
					inner = Util.SequenceActivities(new FlyTimed(ticksUntilTurn), new Fly(self, target), new FlyTimed(ticksUntilTurn));
				else
					inner = Util.SequenceActivities(new Fly(self, target), new FlyTimed(ticksUntilTurn));
			}
			inner = Util.RunActivity(self, inner);

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
