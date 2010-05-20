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
	/* non-turreted attack */
	public class Attack : IActivity
	{
		Actor Target;
		int Range;

		public Attack(Actor target, int range)
		{
			Target = target;
			Range = range;
		}

		public IActivity NextActivity { get; set; }

		public IActivity Tick( Actor self )
		{
			var unit = self.traits.Get<Unit>();

			if (Target == null || Target.IsDead)
				return NextActivity;

			if ((Target.Location - self.Location).LengthSquared >= Range * Range)
				return new Move( Target, Range ) { NextActivity = this };

			var desiredFacing = Util.GetFacing((Target.Location - self.Location).ToFloat2(), 0);
			var renderUnit = self.traits.GetOrDefault<RenderUnit>();
			var numDirs = (renderUnit != null)
				? renderUnit.anim.CurrentSequence.Facings : 8;

			if (Util.QuantizeFacing(unit.Facing, numDirs) 
				!= Util.QuantizeFacing(desiredFacing, numDirs))
			{
				return new Turn( desiredFacing ) { NextActivity = this };
			}

			var attack = self.traits.Get<AttackBase>();
			attack.target = Target;
			attack.DoAttack(self);
			return this;
		}

		public void Cancel(Actor self)
		{
			Target = null;
		}
	}
}
