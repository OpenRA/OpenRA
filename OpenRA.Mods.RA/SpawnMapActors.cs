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
	public class SpawnMapActorsInfo : TraitInfo<SpawnMapActors> { }

	public class SpawnMapActors : IGameStarted
	{
		public Dictionary<string, Actor> MapActors = new Dictionary<string, Actor>();

		public void GameStarted(World world)
		{
			Game.skipMakeAnims = true;		// rude hack

			foreach (var actorReference in world.Map.Actors)
				MapActors[actorReference.Key] = world.CreateActor(actorReference.Value.Type, actorReference.Value.InitDict);

			Game.skipMakeAnims = false;
		}
	}
}
