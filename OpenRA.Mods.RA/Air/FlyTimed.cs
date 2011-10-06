#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Air
{
	public class FlyTimed : Activity
	{
		int remainingTicks;

		public FlyTimed(int ticks) { remainingTicks = ticks; }

		public override Activity Tick(Actor self)
		{
			if( IsCanceled ) return NextActivity;
			var targetAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;
			if (remainingTicks-- == 0) return NextActivity;
			FlyUtil.Fly(self, targetAltitude);
			return this;
		}
	}

	public class FlyOffMap : Activity
	{
		public override Activity Tick(Actor self)
		{
			var targetAltitude = self.Info.Traits.Get<PlaneInfo>().CruiseAltitude;
			if (IsCanceled || !self.World.Map.IsInMap(self.Location))
				return NextActivity;

			FlyUtil.Fly(self, targetAltitude);
			return this;
		}

		public override void Cancel( Actor self )
		{
			base.Cancel(self);
		}
	}
}
