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

namespace OpenRA.Mods.RA.Activities
{
	public class HeliAttack : IActivity
	{
		Actor target;
		public HeliAttack( Actor target ) { this.target = target; }

		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (target == null || target.IsDead)
				return NextActivity;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
			{	
				self.QueueActivity(new HeliReturn());
				return NextActivity;
			}
			var unit = self.traits.Get<Unit>();
			var info = self.Info.Traits.Get<HelicopterInfo>();
			if (unit.Altitude != info.CruiseAltitude)
			{
				unit.Altitude += Math.Sign(info.CruiseAltitude - unit.Altitude);
				return this;
			}

			var range = self.GetPrimaryWeapon().Range - 1;
			var dist = target.CenterLocation - self.CenterLocation;

			var desiredFacing = Util.GetFacing(dist, unit.Facing);
			Util.TickFacing(ref unit.Facing, desiredFacing, self.Info.Traits.Get<UnitInfo>().ROT);

			var mobile = self.traits.WithInterface<IMove>().FirstOrDefault();
			var rawSpeed = .2f * mobile.MovementSpeedForCell(self, self.Location);
			
			if (!float2.WithinEpsilon(float2.Zero, dist, range * Game.CellSize))
				self.CenterLocation += (rawSpeed / dist.Length) * dist;

			return this;
		}

		public void Cancel(Actor self) { target = null; NextActivity = null; }
	}
}
