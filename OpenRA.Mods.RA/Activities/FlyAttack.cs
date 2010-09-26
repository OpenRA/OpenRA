#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	public class FlyAttack : CancelableActivity
	{
		Target Target;

		public FlyAttack(Target target) { Target = target; }

		public override IActivity Tick(Actor self)
		{
			if (IsCanceled) return NextActivity;
			if (!Target.IsValid)
				return NextActivity;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return NextActivity;

			return Util.SequenceActivities(
				Fly.ToPx(Target.CenterLocation),
				new FlyTimed(50),
				this);
		}
	}

	public class FlyCircle : CancelableActivity
	{
		int2 Target;

		public FlyCircle(int2 target) { Target = target; }

		public override IActivity Tick(Actor self)
		{
			if( IsCanceled ) return NextActivity;

			return Util.SequenceActivities(
				Fly.ToPx(Util.CenterOfCell(Target)),
				new FlyTimed(50),
				this);
		}
	}
}
