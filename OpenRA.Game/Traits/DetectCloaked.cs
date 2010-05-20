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

using System.Linq;

namespace OpenRA.Traits
{
	class DetectCloakedInfo : TraitInfo<DetectCloaked>
	{
		public readonly int Interval = 12;		// ~.5s
		public readonly float DecloakTime = 2f;	// 2s
		public readonly int Range = 5;
		public readonly bool AffectOwnUnits = true;
	}

	class DetectCloaked : ITick
	{
		[Sync] int ticks;

		public void Tick(Actor self)
		{
			if (--ticks <= 0)
			{
				var info = self.Info.Traits.Get<DetectCloakedInfo>();
				ticks = info.Interval;

				var toDecloak = self.World.FindUnitsInCircle(self.CenterLocation, info.Range * Game.CellSize)
					.Where(a => a.traits.Contains<Cloak>());

				if (!info.AffectOwnUnits)
					toDecloak = toDecloak.Where(a => self.Owner.Stances[a.Owner] != Stance.Ally);

				foreach (var a in toDecloak)
					a.traits.Get<Cloak>().Decloak((int)(25 * info.DecloakTime));
			}
		}
	}

	class RenderRangeCircleInfo : TraitInfo<RenderRangeCircle> { }
	class RenderRangeCircle { }
}
