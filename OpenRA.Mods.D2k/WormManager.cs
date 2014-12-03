#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.D2k
{
	[Desc("Controls the spawning of sandworms. Attach this to the world actor.")]
	class WormManagerInfo : ITraitInfo
	{
		[Desc("Minimum number of worms")]
		public readonly int Minimum = 2;

		[Desc("Maximum number of worms")]
		public readonly int Maximum = 4;

		[Desc("Average time (seconds) between worm spawn")]
		public readonly int SpawnInterval = 120;

		public readonly string WormSignNotification = "WormSign";

		public readonly string WormSignature = "sandworm";
		public readonly string WormOwnerPlayer = "Creeps";

		public object Create(ActorInitializer init) { return new WormManager(this, init.self); }
	}

	class WormManager : ITick
	{
		int countdown;
		int wormsPresent;
		readonly WormManagerInfo info;
		readonly Lazy<Actor[]> spawnPoints;
		readonly Lazy<RadarPings> radarPings;

		public WormManager(WormManagerInfo info, Actor self)
		{
			this.info = info;
			radarPings = Exts.Lazy(() => self.World.WorldActor.Trait<RadarPings>());
			spawnPoints = Exts.Lazy(() => self.World.ActorsWithTrait<WormSpawner>().Select(x => x.Actor).ToArray());
		}

		public void Tick(Actor self)
		{
			// TODO: Add a lobby option to disable worms just like crates

			// TODO: It would be even better to stop 
			if (!spawnPoints.Value.Any())
				return;

			// Apparantly someone doesn't want worms or the maximum number of worms has been reached
			if (info.Maximum < 1 || wormsPresent >= info.Maximum)
				return;

			if (--countdown > 0 && wormsPresent >= info.Minimum)
				return;

			countdown = info.SpawnInterval * 25;

			var wormLocations = new List<WPos>();

			wormLocations.Add(SpawnWorm(self));
			while (wormsPresent < info.Minimum)
				wormLocations.Add(SpawnWorm(self));

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
			return spawnPoints.Value.Random(self.World.SharedRandom);
		}

		public void DecreaseWorms()
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

	[Desc("An actor with this trait indicates a valid spawn point for sandworms.")]
	class WormSpawnerInfo : TraitInfo<WormSpawner> { }
	class WormSpawner { }
}
