#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SpawnMPUnitsInfo : ITraitInfo, Requires<MPStartLocationsInfo>
	{
		public readonly string InitialUnit = "mcv";

		public object Create (ActorInitializer init) { return new SpawnMPUnits(this); }
	}

	class SpawnMPUnits : IWorldLoaded
	{
		SpawnMPUnitsInfo info;

		public SpawnMPUnits(SpawnMPUnitsInfo info) { this.info = info; }

		public void WorldLoaded(World world)
		{
			foreach (var s in world.WorldActor.Trait<MPStartLocations>().Start)
				SpawnUnitsForPlayer(s.Key, s.Value);
		}

		void SpawnUnitsForPlayer(Player p, int2 sp)
		{
			if (!p.PlayerReference.DefaultStartingUnits)
				return;	/* they don't want an mcv, the map provides something else for them */

			p.World.CreateActor(info.InitialUnit, new TypeDictionary
			{
				new LocationInit( sp ),
				new OwnerInit( p ),
			});
		}
	}
}
