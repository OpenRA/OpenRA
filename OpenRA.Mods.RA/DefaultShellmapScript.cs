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

using OpenRA.Mods.RA.Effects;
using OpenRA.Traits;
using OpenRA;
using System.Linq;
using System.Collections.Generic;
using System;

namespace OpenRA.Mods.RA
{
	class DefaultShellmapScriptInfo : ITraitInfo
	{
		public object Create(Actor self) { return new DefaultShellmapScript(); }
	}

	class DefaultShellmapScript: ILoadWorldHook, ITick
	{		
		Player goodguy;
		Player greece;
		Dictionary<string, Actor> MapActors;
		
		public void WorldLoaded(World w)
		{
			Game.MoveViewport((.5f * (w.Map.TopLeft + w.Map.BottomRight).ToFloat2()).ToInt2());
			// Sound.PlayMusic("hell226m.aud");
			goodguy = w.players.Values.Where(x => x.InternalName == "GoodGuy").FirstOrDefault();
			greece = w.players.Values.Where(x => x.InternalName == "Greece").FirstOrDefault();
			MapActors = w.WorldActor.traits.Get<SpawnMapActors>().MapActors;
			
			
			goodguy.Stances[greece] = Stance.Enemy;
			greece.Stances[goodguy] = Stance.Enemy;
		}
		
		int ticks = 0;
		public void Tick(Actor self)
		{
			if (ticks == 100)
				MapActors["mslo1"].traits.Get<NukeSilo>().Attack(new int2(96,53));
			if (ticks == 110)
				MapActors["mslo2"].traits.Get<NukeSilo>().Attack(new int2(92,53));
			if (ticks == 120)
				MapActors["mslo3"].traits.Get<NukeSilo>().Attack(new int2(94,50));
			
			ticks++;
		}
	}
	

}
