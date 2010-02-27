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

namespace OpenRA.Traits
{
	class TurretedInfo : ITraitInfo
	{
		public readonly int ROT = 255;
		public readonly int InitialFacing = 128;

		public object Create(Actor self) { return new Turreted(self); }
	}

	class Turreted : ITick
	{
		[Sync]
		public int turretFacing = 0;
		public int? desiredFacing;

		public Turreted(Actor self)
		{
			turretFacing = self.Info.Traits.Get<TurretedInfo>().InitialFacing;
		}

		public void Tick( Actor self )
		{
			var df = desiredFacing ?? ( self.traits.Contains<Unit>() ? self.traits.Get<Unit>().Facing : turretFacing );
			Util.TickFacing(ref turretFacing, df, self.Info.Traits.Get<TurretedInfo>().ROT);
		}
	}
}
