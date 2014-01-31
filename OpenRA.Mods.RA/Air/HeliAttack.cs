#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class HeliAttack : Activity
	{
		Target target;
		public HeliAttack(Target target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			var reloads = self.TraitOrDefault<Reloads>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo() && reloads == null)
				return Util.SequenceActivities(new HeliReturn(), NextActivity);

			var helicopter = self.Trait<Helicopter>();
			var attack = self.Trait<AttackHeli>();
			var dist = target.CenterPosition - self.CenterPosition;

			// Can rotate facing while ascending
			var desiredFacing = Util.GetFacing(dist, helicopter.Facing);
			helicopter.Facing = Util.TickFacing(helicopter.Facing, desiredFacing, helicopter.ROT);

			if (HeliFly.AdjustAltitude(self, helicopter, helicopter.Info.CruiseAltitude))
				return this;

			// Fly towards the target
			if (!target.IsInRange(self.CenterPosition, attack.GetMaximumRange()))
				helicopter.SetPosition(self, helicopter.CenterPosition + helicopter.FlyStep(desiredFacing));

			attack.DoAttack(self, target);

			return this;
		}
	}
}
