#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class HeliAttack : CancelableActivity
	{
		Target target;
		public HeliAttack( Target target ) { this.target = target; }

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (!target.IsValid) return NextActivity;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
			{	
				self.QueueActivity(new HeliReturn());
				return NextActivity;
			}
			
			var aircraft = self.Trait<Aircraft>();
			var info = self.Info.Traits.Get<HelicopterInfo>();
			if (aircraft.Altitude != info.CruiseAltitude)
			{
				aircraft.Altitude += Math.Sign(info.CruiseAltitude - aircraft.Altitude);
				return this;
			}

			var attack = self.Trait<AttackBase>();
			var range = attack.GetMaximumRange() - 1;
			var dist = target.CenterLocation - self.CenterLocation;

			var desiredFacing = Util.GetFacing(dist, aircraft.Facing);
			aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.ROT);
			var rawSpeed = .2f * aircraft.MovementSpeed;
			
			if (!float2.WithinEpsilon(float2.Zero, dist, range * Game.CellSize))
				aircraft.center += (rawSpeed / dist.Length) * dist;

			attack.DoAttack( self, target );

			return this;
		}
	}
}
