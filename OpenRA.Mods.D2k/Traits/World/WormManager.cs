#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k.Traits
{
	[Desc("Controls the spawning of sandworms. Attach this to the world actor.")]
	class WormManagerInfo : ITraitInfo
	{
		[Desc("Minimum number of worms")]
		public readonly int Minimum = 2;

		[Desc("Maximum number of worms")]
		public readonly int Maximum = 4;

		[Desc("Time (in ticks) between worm spawn.")]
		public readonly int SpawnInterval = 3000;

		[Desc("Name of the actor that will be spawned.")]
		public readonly string WormSignature = "sandworm";

		public readonly string WormSignNotification = "WormSign";
		public readonly string WormOwnerPlayer = "Creeps";

		public object Create(ActorInitializer init) { return new WormManager(init.Self, this); }
	}

	class WormManager : ITick
	{
		readonly WormManagerInfo info;
		readonly Lazy<Actor[]> spawnPointActors;
		readonly Lazy<RadarPings> radarPings;

		int spawnCountdown;
		int wormsPresent;

		public WormManager(Actor self, WormManagerInfo info)
		{
			this.info = info;
			radarPings = Exts.Lazy(() => self.World.WorldActor.Trait<RadarPings>());
			spawnPointActors = Exts.Lazy(() => self.World.ActorsWithTrait<WormSpawner>().Select(x => x.Actor).ToArray());
		}

		public void Tick(Actor self)
		{
			if (!self.World.LobbyInfo.GlobalSettings.Creeps)
				return;

			if (!spawnPointActors.Value.Any())
				return;

			// Apparantly someone doesn't want worms or the maximum number of worms has been reached
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

			AnnounceWormSign(self, wormLocations);
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

		void AnnounceWormSign(Actor self, IEnumerable<WPos> wormLocations)
		{
			if (self.World.LocalPlayer != null)
				Sound.PlayNotification(self.World.Map.Rules, self.World.LocalPlayer, "Speech", info.WormSignNotification, self.World.LocalPlayer.Country.Race);

			if (radarPings.Value == null)
				return;

			foreach (var wormLocation in wormLocations)
				radarPings.Value.Add(() => true, wormLocation, Color.Red, 50);
		}
	}
}
