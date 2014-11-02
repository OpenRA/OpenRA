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
using System.Linq;
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
        public readonly int Maximum = 5;

        [Desc("Average time (seconds) between crate spawn")]
        public readonly int SpawnInterval = 180;

        public readonly string WormSignature = "sandworm";
        public readonly string WormOwnerPlayer = "Creeps";

        public object Create (ActorInitializer init) { return new WormManager(this, init.self); }
    }

    class WormManager : ITick
    {
        int countdown;
        int wormsPresent;
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

            if (--countdown > 0)
                return;

            countdown = info.SpawnInterval * 25;
            if (wormsPresent < info.Maximum)
                SpawnWorm(self);
        }

        private void SpawnWorm (Actor self)
        {
            var spawnLocation = GetRandomSpawnPosition(self);
            self.World.AddFrameEndTask(w =>
                        w.CreateActor(info.WormSignature, new TypeDictionary
                                                            {
                                                                new OwnerInit(w.Players.First(x => x.PlayerName == info.WormOwnerPlayer)),
                                                                new LocationInit(spawnLocation)
                                                            }));
            wormsPresent++;
        }

        private CPos GetRandomSpawnPosition(Actor self)
        {
            // TODO: This is here only for testing, while the maps don't have valid spawn points
            if (!spawnPoints.Value.Any())
                return self.World.Map.ChooseRandomEdgeCell(self.World.SharedRandom);

            return spawnPoints.Value[self.World.SharedRandom.Next(0, spawnPoints.Value.Count() - 1)].Location;
        }

        public void DecreaseWorms()
        {
            wormsPresent--;
        }
    }
}
