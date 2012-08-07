#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	class Allies02ScriptInfo : TraitInfo<Allies02Script>, Requires<SpawnMapActorsInfo> { }

	class Allies02Script : IWorldLoaded, ITick
	{
		Actor chinookHusk;
		Actor sam1;
		Actor sam2;
		Actor sam3;

		Player allies;
		Player soviets;

		public void Tick(Actor self)
		{

		}

		public void WorldLoaded(World w)
		{
			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			chinookHusk = actors["ChinookHusk"];
			sam1 = actors["SAM1"];
			sam2 = actors["SAM2"];
			sam3 = actors["SAM3"];
			w.WorldActor.Trait<Shroud>().Explore(w, sam1.Location, 3);
			w.WorldActor.Trait<Shroud>().Explore(w, sam2.Location, 3);
			w.WorldActor.Trait<Shroud>().Explore(w, sam3.Location, 3);
		}
	}
}
