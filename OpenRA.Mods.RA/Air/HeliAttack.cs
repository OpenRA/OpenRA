#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class HeliAttack : Activity
	{
		Target target;
		public HeliAttack( Target target ) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (!target.IsValid) return NextActivity;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			var reloads = self.TraitOrDefault<Reloads>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo() && reloads == null)
				return Util.SequenceActivities( new HeliReturn(), NextActivity );

			var aircraft = self.Trait<Aircraft>();
			var info = self.Info.Traits.Get<HelicopterInfo>();
			if (aircraft.Altitude != info.CruiseAltitude)
			{
				aircraft.Altitude += Math.Sign(info.CruiseAltitude - aircraft.Altitude);
				return this;
			}

			var attack = self.Trait<AttackHeli>();
			var dist = target.CenterLocation - self.CenterLocation;

			var desiredFacing = Util.GetFacing(dist, aircraft.Facing);
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.ROT);

			if (!target.IsInRange(self.CenterPosition, attack.GetMaximumRange()))
				aircraft.TickMove(PSubPos.PerPx * aircraft.MovementSpeed, desiredFacing);

			attack.DoAttack( self, target );

			return this;
		}
	}
}
