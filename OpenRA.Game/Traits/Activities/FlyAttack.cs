#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

namespace OpenRA.Traits.Activities
{
	public class FlyAttack : IActivity
	{
		public IActivity NextActivity { get; set; }
		Actor Target;

		public FlyAttack(Actor target) { Target = target; }

		public IActivity Tick(Actor self)
		{
			if (Target == null || Target.IsDead) 
				return NextActivity;

			var limitedAmmo = self.traits.GetOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return NextActivity;

			return Util.SequenceActivities(
				new Fly(Target.CenterLocation),
				new FlyTimed(50),
				this);
		}

		public void Cancel(Actor self) { Target = null; NextActivity = null; }
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
