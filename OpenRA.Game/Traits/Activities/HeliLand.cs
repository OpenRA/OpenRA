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

using OpenRA.GameRules;
namespace OpenRA.Traits.Activities
{
	class HeliLand : IActivity
	{
		public HeliLand(bool requireSpace) { this.requireSpace = requireSpace; }

		bool requireSpace;
		bool isCanceled;
		public IActivity NextActivity { get; set; }

		public IActivity Tick(Actor self)
		{
			if (isCanceled) return NextActivity;
			var unit = self.traits.Get<Unit>();
			if (unit.Altitude == 0)
				return NextActivity;

			if (requireSpace && !self.World.IsPathableCell(self.Location, UnitMovementType.Foot))
				return this;	// fail to land if no space

			--unit.Altitude;
			return this;
		}

		public void Cancel(Actor self) { isCanceled = true; NextActivity = null; }
	}
}
