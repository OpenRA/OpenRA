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
	public class FlyAttack : IActivity
	{
		public IActivity NextActivity { get; set; }
		Target Target;

		public FlyAttack(Target target) { Target = target; }

		public IActivity Tick(Actor self)
		{
			if (!Target.IsValid)
				return NextActivity;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return NextActivity;

			return Util.SequenceActivities(
				new Fly(Target.CenterLocation),
				new FlyTimed(50),
				this);
		}

		public void Cancel(Actor self) { Target = Target.None; NextActivity = null; }
	}

	public class FlyCircle : IActivity
	{
		public IActivity NextActivity { get; set; }
		int2 Target;
		bool isCanceled;

		public FlyCircle(int2 target) { Target = target; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled)
				return NextActivity;

			return Util.SequenceActivities(
				new Fly(Util.CenterOfCell(Target)),
				new FlyTimed(50),
				this);
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
