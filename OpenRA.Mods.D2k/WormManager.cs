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
using System.Drawing;
using System.Linq;
using OpenRA.Mods.Common;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.D2k
{
	[Desc("Controls the spawning of sandworms. Attach this to the world actor.")]
	class WormManagerInfo : ITraitInfo
	{
		[Desc("Minimum number of worms")]
		public readonly int Minimum = 1;

		[Desc("Maximum number of worms")]
		public readonly int Maximum = 8;

		[Desc("Average time (seconds) between worm spawn")]
		public readonly int SpawnInterval = 180;

		public readonly string WormSignNotification = "WormSign";

		public readonly string WormSignature = "sandworm";
		public readonly string WormOwnerPlayer = "Creeps";

		public object Create (ActorInitializer init) { return new WormManager(this, init.self); }
	}

	class WormManager : ITick
	{
		int countdown;
		int wormsPresent;
		RadarPings radarPings;
		readonly WormManagerInfo info;
		readonly Lazy<Actor[]> spawnPoints;

		public WormManager(WormManagerInfo info, Actor self)
		{
			this.info = info;
			spawnPoints = Exts.Lazy(() => self.World.ActorsWithTrait<WormSpawner>().Select(x => x.Actor).ToArray());
		}

		public void Tick(Actor self)
		{
			// TODO: Add a lobby option to disable worms just like crates

			// TODO: It would be even better to stop 
			if (!spawnPoints.Value.Any())
				return;

			if (--countdown > 0)
				return;

			countdown = info.SpawnInterval * 25;
			if (wormsPresent < info.Maximum)
				SpawnWorm(self);
		}

		void SpawnWorm (Actor self)
		{
			var spawnPosition = GetRandomSpawnPosition(self);
			var spawnLocation = self.World.Map.CellContaining(spawnPosition);
			self.World.AddFrameEndTask(w => w.CreateActor(info.WormSignature, new TypeDictionary
			{
				new OwnerInit(w.Players.First(x => x.PlayerName == info.WormOwnerPlayer)),
				new LocationInit(spawnLocation)
			}));
			wormsPresent++;

			AnnounceWormSign(self, spawnPosition);
		}

		WPos GetRandomSpawnPosition(Actor self)
		{
			return spawnPoints.Value.Random(self.World.SharedRandom).CenterPosition;
		}

		public void DecreaseWorms()
		{
			wormsPresent--;
		}

		void AnnounceWormSign(Actor self, WPos wormSpawnPosition)
		{
			if (self.World.LocalPlayer == null)
				return;
			
			Sound.PlayNotification(self.World.Map.Rules, self.World.LocalPlayer, "Speech", info.WormSignNotification, self.World.LocalPlayer.Country.Race);

			if (radarPings == null)
			{
				if (self.World.WorldActor == null)
					return;

				radarPings = self.World.WorldActor.TraitOrDefault<RadarPings>();
			}
			
			radarPings.Add(() => true, wormSpawnPosition, Color.Red, 50);
		}
	}

	[Desc("An actor with this trait indicates a valid spawn point for sandworms.")]
	class WormSpawnerInfo : TraitInfo<WormSpawner> { }
	class WormSpawner { }
}
