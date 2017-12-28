#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Controls the spawning of specified actor types. Attach this to the world actor.")]
	public class ActorSpawnManagerInfo : ConditionalTraitInfo, Requires<MapCreepsInfo>
	{
		[Desc("Minimum number of actors.")]
		public readonly int Minimum = 0;

		[Desc("Maximum number of actors.")]
		public readonly int Maximum = 4;

		[Desc("Time (in ticks) between actor spawn.")]
		public readonly int SpawnInterval = 6000;

		[FieldLoader.Require]
		[ActorReference]
		[Desc("Name of the actor that will be randomly picked to spawn.")]
		public readonly string[] Actors = { };

		public readonly string Owner = "Creeps";

		[Desc("Type of ActorSpawner with which it connects.")]
 		public readonly HashSet<string> Types = new HashSet<string>() { };

		public override object Create(ActorInitializer init) { return new ActorSpawnManager(init.Self, this); }
	}

	public class ActorSpawnManager : ConditionalTrait<ActorSpawnManagerInfo>, ITick, INotifyCreated
	{
		readonly ActorSpawnManagerInfo info;
		TraitPair<ActorSpawner>[] spawnPointActors;

		bool enabled;
		int spawnCountdown;
		int actorsPresent;

		public ActorSpawnManager(Actor self, ActorSpawnManagerInfo info) : base(info)
		{
			this.info = info;
		}

		void INotifyCreated.Created(Actor self)
		{
			self.World.AddFrameEndTask(w =>
			{
				spawnPointActors = w.ActorsWithTrait<ActorSpawner>()
					.Where(x => info.Types.Overlaps(x.Trait.Types) || !x.Trait.Types.Any())
					.ToArray();

				enabled = self.Trait<MapCreeps>().Enabled && spawnPointActors.Any();
			});
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitDisabled || !enabled)
				return;

			if (info.Maximum < 1 || actorsPresent >= info.Maximum)
				return;

			if (--spawnCountdown > 0 && actorsPresent >= info.Minimum)
				return;

			spawnCountdown = info.SpawnInterval;

			do
			{
				// Always spawn at least one actor, plus
				// however many needed to reach the minimum.
				SpawnActor(self);
			} while (actorsPresent < info.Minimum);
		}

		WPos SpawnActor(Actor self)
		{
			var spawnPoint = GetRandomSpawnPoint(self.World.SharedRandom);
			self.World.AddFrameEndTask(w => w.CreateActor(info.Actors.Random(self.World.SharedRandom), new TypeDictionary
			{
				new OwnerInit(w.Players.First(x => x.PlayerName == info.Owner)),
				new LocationInit(spawnPoint.Location)
			}));

			actorsPresent++;

			return spawnPoint.CenterPosition;
		}

		Actor GetRandomSpawnPoint(Support.MersenneTwister random)
		{
			return spawnPointActors.Random(random).Actor;
		}

		public void DecreaseActorCount()
		{
			actorsPresent--;
		}
	}
}
