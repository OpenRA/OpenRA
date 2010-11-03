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
using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class Fly : CancelableActivity
	{
		public readonly float2 Pos;

		private Fly( float2 px ) { Pos = px; }
		
		public static Fly ToPx( float2 px ) { return new Fly( px ); }
		public static Fly ToCell( int2 pos ) { return new Fly( Util.CenterOfCell( pos ) ); }

		public override IActivity Tick(Actor self)
		{
			var cruiseAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;

			if (IsCanceled) return NextActivity;

			var d = Pos - self.CenterLocation;
			if (d.LengthSquared < 50)		/* close enough */
				return NextActivity;

			var aircraft = self.Trait<Aircraft>();

			var desiredFacing = Util.GetFacing(d, aircraft.Facing);
			if (aircraft.Altitude == cruiseAltitude)
				aircraft.Facing = Util.TickFacing(aircraft.Facing, desiredFacing, aircraft.ROT);

			if (aircraft.Altitude < cruiseAltitude)
				++aircraft.Altitude;

			FlyUtil.Fly(self, cruiseAltitude);
			return this;
		}

		public override IEnumerable<float2> GetCurrentPath()
		{
			yield return Pos;
		}
	}

	public static class FlyUtil
	{
		public static void Fly(Actor self, int desiredAltitude )
		{
			var aircraft = self.Trait<Aircraft>();
			var speed = .2f * aircraft.MovementSpeed;
			var angle = aircraft.Facing / 128f * Math.PI;
			aircraft.center += speed * -float2.FromAngle((float)angle);
			aircraft.Altitude += Math.Sign(desiredAltitude - aircraft.Altitude);
		}
	}
}
