#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class SpawnMapActorsInfo : TraitInfo<SpawnMapActors> { }

	public class SpawnMapActors : IWorldLoaded
	{
		public Dictionary<string, Actor> Actors = new Dictionary<string, Actor>();

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			foreach (var actorReference in world.Map.Actors.Value)
			{
				// if there is no real player associated, dont spawn it.
				var ownerName = actorReference.Value.InitDict.Get<OwnerInit>().PlayerName;
				if (!world.Players.Any(p => p.InternalName == ownerName))
					continue;

				var initDict = actorReference.Value.InitDict;
				initDict.Add(new SkipMakeAnimsInit());
				Actors[actorReference.Key] = world.CreateActor(actorReference.Value.Type, initDict);
			}
		}
	}

	public class SkipMakeAnimsInit : IActorInit {}
}
