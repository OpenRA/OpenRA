#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class SpawnMPUnitsInfo : TraitInfo<SpawnMPUnits>, ITraitPrerequisite<MPStartLocationsInfo> {}

	class SpawnMPUnits : IGameStarted
	{
		public void GameStarted(World world)
		{
			foreach (var s in world.WorldActor.Trait<MPStartLocations>().Start)
				SpawnUnitsForPlayer(s.Key, s.Value);
		}

		void SpawnUnitsForPlayer(Player p, int2 sp)
		{
			p.World.CreateActor("mcv", new TypeDictionary 
			{ 
				new LocationInit( sp ),
				new OwnerInit( p ),
			});
		}
	}
}
