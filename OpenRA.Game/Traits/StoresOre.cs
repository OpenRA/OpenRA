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

using System.Collections.Generic;

namespace OpenRA.Traits
{
	class StoresOreInfo : TraitInfo<StoresOre>
	{
		public readonly int Pips = 0;
		public readonly int Capacity = 0;
	}

	class StoresOre : IPips, IAcceptThief
	{
		public void OnSteal(Actor self, Actor thief)
		{
			// Steal half the ore the building holds
			var toSteal = self.Info.Traits.Get<StoresOreInfo>().Capacity / 2;
			self.Owner.PlayerActor.traits.Get<PlayerResources>().TakeCash(toSteal);
			thief.Owner.PlayerActor.traits.Get<PlayerResources>().GiveCash(toSteal);
			
			var eva = thief.World.WorldActor.Info.Traits.Get<EvaAlertsInfo>();
			Sound.PlayToPlayer(thief.Owner, eva.CreditsStolen);
		}
		
		public IEnumerable<PipType> GetPips(Actor self)
		{
			var numPips = self.Info.Traits.Get<StoresOreInfo>().Pips;

			return Graphics.Util.MakeArray( numPips, 
				i => (self.World.LocalPlayer.PlayerActor.traits.Get<PlayerResources>().GetSiloFullness() > i * 1.0f / numPips) 
					? PipType.Yellow : PipType.Transparent );
		}
	}
}
