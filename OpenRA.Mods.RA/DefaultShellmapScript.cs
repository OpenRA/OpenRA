#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class DefaultShellmapScriptInfo : TraitInfo<DefaultShellmapScript> { }

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
			if (ticks == 250)
			{
				MapActors["pdox"].traits.Get<Chronosphere>().Teleport(MapActors["ca1"], new int2(90, 70));
				MapActors["pdox"].traits.Get<Chronosphere>().Teleport(MapActors["ca2"], new int2(92, 71));
			}
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
