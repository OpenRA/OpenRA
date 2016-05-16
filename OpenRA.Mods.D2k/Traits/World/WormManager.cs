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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Controls the spawning of sandworms. Attach this to the world actor.")]
	class WormManagerInfo : ITraitInfo, Requires<MapCreepsInfo>
	{
		[Desc("Minimum number of worms")]
		public readonly int Minimum = 0;

		[Desc("Maximum number of worms")]
		public readonly int Maximum = 4;

		[Desc("Time (in ticks) between worm spawn.")]
		public readonly int SpawnInterval = 6000;

		[Desc("Name of the actor that will be spawned.")]
		public readonly string WormSignature = "sandworm";

		public readonly string WormSignNotification = "WormSign";
		public readonly string WormOwnerPlayer = "Creeps";

		public object Create(ActorInitializer init) { return new WormManager(init.Self, this); }
	}

	class WormManager : ITick, INotifyCreated
	{
		readonly WormManagerInfo info;
		readonly Lazy<Actor[]> spawnPointActors;

		bool enabled;
		int spawnCountdown;
		int wormsPresent;

		public WormManager(Actor self, WormManagerInfo info)
		{
			this.info = info;
			spawnPointActors = Exts.Lazy(() => self.World.ActorsHavingTrait<WormSpawner>().ToArray());
		}

		void INotifyCreated.Created(Actor self)
		{
			enabled = self.Trait<MapCreeps>().Enabled;
		}

		public void Tick(Actor self)
		{
			if (!enabled)
				return;

			if (!spawnPointActors.Value.Any())
				return;

			// Apparently someone doesn't want worms or the maximum number of worms has been reached
			if (info.Maximum < 1 || wormsPresent >= info.Maximum)
				return;

			if (--spawnCountdown > 0 && wormsPresent >= info.Minimum)
				return;

			spawnCountdown = info.SpawnInterval;

			var wormLocations = new List<WPos>();

			do
			{
				// Always spawn at least one worm, plus however many
				// more we need to reach the defined minimum count.
				wormLocations.Add(SpawnWorm(self));
			} while (wormsPresent < info.Minimum);
		}

		WPos SpawnWorm(Actor self)
		{
			var spawnPoint = GetRandomSpawnPoint(self);
			self.World.AddFrameEndTask(w => w.CreateActor(info.WormSignature, new TypeDictionary
			{
				new OwnerInit(w.Players.First(x => x.PlayerName == info.WormOwnerPlayer)),
				new LocationInit(spawnPoint.Location)
			}));

			wormsPresent++;

			return spawnPoint.CenterPosition;
		}

		Actor GetRandomSpawnPoint(Actor self)
		{
			return spawnPointActors.Value.Random(self.World.SharedRandom);
		}

		public void DecreaseWormCount()
		{
			wormsPresent--;
		}
	}
}
