#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Spawns the initial units for each player upon game start.")]
	public class SpawnMapActorsInfo : TraitInfo<SpawnMapActors> { }

	public class SpawnMapActors : IWorldLoaded
	{
		public Dictionary<string, Actor> Actors = new Dictionary<string, Actor>();
		public uint LastMapActorID { get; private set; }

		public void WorldLoaded(World world, WorldRenderer wr)
		{
			foreach (var kv in world.Map.ActorDefinitions)
			{
				var actorReference = new ActorReference(kv.Value.Value, kv.Value.ToDictionary());

				// If there is no real player associated, don't spawn it.
				var ownerName = actorReference.InitDict.Get<OwnerInit>().PlayerName;
				if (!world.Players.Any(p => p.InternalName == ownerName))
					continue;

				var initDict = actorReference.InitDict;
				initDict.Add(new SkipMakeAnimsInit());
				initDict.Add(new SpawnedByMapInit(kv.Key));
				var actor = world.CreateActor(actorReference.Type, initDict);
				Actors[kv.Key] = actor;
				LastMapActorID = actor.ActorID;
			}
		}
	}

	public class SkipMakeAnimsInit : IActorInit, ISuppressInitExport { }
	public class SpawnedByMapInit : IActorInit<string>, ISuppressInitExport
	{
		public readonly string Name;
		public SpawnedByMapInit(string name) { Name = name; }

		public string Value(World world)
		{
			return Name;
		}
	}
}
