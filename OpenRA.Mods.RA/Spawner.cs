using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
    public class Wave
    {
        [Desc("When AutoStart time runs out should alive wave units be killed?")]
        public readonly bool AutoKill = false;
        [Desc("Automatically end round after x ticks.")]
        public readonly int AutoStart = -1;
        [Desc("How much delay before wave will start.")]
        public readonly int Delay = 0;
        [Desc("How much delay in the spawn cycle process.")]
        public readonly int DelayBetweenSubSpawn = 30;
        [Desc("Takes a list of unit names that should be spawned. Example: Units: E1, E2, E3. It can also copy a unit at certain location. To do that you enter instead of unit name {x|y}. Example: Units: {25|25}, E2, E3")]
        public readonly string[] Units;
        [Desc("Takes a list of integers representing amount of units of each type. Example: Units: E1, E3 Amounts: 2, 5. This exmaple would spawn 2 riflemen and 5 rocket guys.")]
        public readonly int[] Amounts;
        [Desc("Takes a list of player names. This is for who will be the owner of the units. Example: Units: E1, E3 Amounts: 2, 5 Owners: Multi0, Multi0. This exmaple would spawn 2 riflemen and 5 rocket guys for Multi0.")]
        public readonly string[] Owners;
        [Desc("How many times do you want to repeat this wave? Default is 0.")]
        public readonly int RepeatCount = 0;
        [Desc("Send a message before wave starts")]
        public readonly string MessageBegin = "";
        [Desc("Send a message when wave begin")]
        public readonly string MessageDidBegin = "";
        [Desc("Send a message when wave is ended")]
        public readonly string MessageEnded = "";
        [Desc("Wait for units to be killed before starting next wave? Take a boolean. Default is false.")]
        public readonly bool Clear = false;
        [Desc("Maximum number of units that can be alive simultaneously. It will wait and create the new ones as soon as other dies.")]
        public readonly int MaxUnits = -1;
        [Desc("Takes a list of player names. Gives cash to given players after wave is completed.")]
        public readonly string[] PlayersToGiveCash;
        [Desc("Takes a list of integers representing the amount of cash to give for each player. Example: PlayersToGiveCash: Multi0, Multi1 PlayersToGiveCashAmount: 100, 200. This would give 100 to Multi0 and 200 to Mulit1.")]
        public readonly int[] PlayersToGiveCashAmount;
    }

    public class SpawnerInfo : ITraitInfo
    {
        [FieldLoader.LoadUsing("LoadWaves")]
        public readonly Wave[] Waves = null;

        [Desc("How many times to repeat all waves.")]
        public readonly int RepeatCount = 0;
        [Desc("Inital delay before spawner will start.")]
        public readonly int Delay = 0;
        [Desc("Should only one message will be sent for all spawners? Default false.")]
        public readonly bool IsSynced = false;
        [Desc("All spawners share units? Default false.")]
        public readonly bool SharedUnits = false;
        [Desc("A list of players that will win after the waves are complete.")]
        public readonly string[] PlayersWin = null;
        [Desc("A list of players that will lose after the waves are complete.")]
        public readonly string[] PlayersLose = null;
        [Desc("The given actor needs to be alive for spawner to work.")]
        public readonly string[] ActorsAlive = null;

        static object LoadWaves(MiniYaml x)
        {
            return x.Nodes.Where(y => y.Key.Split('@')[0] == "Wave")
                .Select(y => FieldLoader.Load<Wave>(y.Value))
                .ToArray();
        }

        public object Create(ActorInitializer init) { return new Spawner(init.self, this); }
    }

    public class Spawner : ITick
    {
        public SpawnerInfo Info;
        World world;

        public Spawner(Actor self, SpawnerInfo info)
        {
            Info = info;
            spawnRepeat = info.RepeatCount;
            spawnedUnits = new List<Actor>();
        }

        public void stopSpawner()
        {
            state = State.Complete;
            delay = 0;
            currentWaveIndex = 0;
        }

        public void completeSpawner()
        {
            stopSpawner();
            if (Info.PlayersWin != null)
                foreach (string playerName in Info.PlayersWin)
                {
                    var player = world.Players.Where(p => p.InternalName == playerName).Last();
                    if (player != null)
                        player.WinState = WinState.Won;
                }
            if (Info.PlayersLose != null)
                foreach (string playerName in Info.PlayersLose)
                {
                    var player = world.Players.Where(p => p.InternalName == playerName).Last();
                    if (player != null)
                        player.WinState = WinState.Lost;
                }
        }

        public void repeatSpawn()
        {
            state = State.Initial;
            delay = 0;
            currentWaveIndex = 0;
        }

        public void repeatWave()
        {
            internalWave = 0;
            waveRepeat--;
            unitsToSpawn = 0;
        }

        public void completeWave()
        {
            internalWave = 0;
            currentWaveIndex++;
            state = State.WaveSpawn;
            delay = 0;
            unitsToSpawn = 0;
        }

        public void startWave()
        {
            internalWave = 0;
            delay = currentWave().Delay;
            state = State.WaveStart;
            waveRepeat = currentWave().RepeatCount;
            autoStart = currentWave().AutoStart;
            didBegin = false;
            if (currentWave().MessageBegin.Length > 0 && isMaster)
                Game.AddChatLine(System.Drawing.Color.Red, "Info", currentWave().MessageBegin);
        }

        int currentWaveIndex = 0;
        public Wave currentWave()
        {
            if (Info.Waves.Length > currentWaveIndex)
                return Info.Waves[currentWaveIndex];
            return null;
        }

        public void spawnUnit(CPos location)
        {
            var wave = currentWave();
            string owner = null;
            if (wave.Owners.Length <= internalWave)
                owner = wave.Owners[wave.Owners.Length - 1];
            else
                owner = wave.Owners[internalWave];

            string actorName = null;
            if (wave.Units.Length <= internalWave)
                actorName = wave.Units[wave.Units.Length - 1];
            else
                actorName = wave.Units[internalWave];
            if (actorName.Length > 0 && actorName[0] == '{')
            {
                actorName = actorName.Substring(1);
                var posOfEnd = actorName.IndexOf("}");
                if (posOfEnd != -1)
                    actorName = actorName.Substring(0, posOfEnd);
                var coordinates = actorName.Split('|');
                if (coordinates.Length == 2)
                {
                    var x = int.Parse(coordinates[0]);
                    var y = int.Parse(coordinates[1]);
                    var actors = world.ActorMap.GetUnitsAt(new CPos(x, y)).ToArray();
                    if (actors.Length > 0)
                        actorName = actors[0].Info.Name;
                }
            }

            if (Info.SharedUnits && useMaster && isMaster)
            {
                var traitPairs = world.ActorsWithTrait<Spawner>().ToArray();
                var canSpawn = Math.Min(traitPairs.Length, unitsToSpawn);
                for (int i = 0; i < canSpawn; i++)
                {
                    if (shouldSpawnActorForPlayer(owner, actorName))
                    {
                        Actor actor = world.CreateActor(actorName, new TypeDictionary
                        {
                            new OwnerInit(owner),
                            new LocationInit(traitPairs[i].Actor.Location)
                        });
                        spawnedUnits.Add(actor);
                    }
                }
                unitsToSpawn -= canSpawn;
                if (unitsToSpawn <= 0)
                    internalWave++;
            }
            else if (!useMaster || !Info.SharedUnits)
            {
                if (shouldSpawnActorForPlayer(owner, actorName))
                {
                    Actor actor = world.CreateActor(actorName, new TypeDictionary
                    {
                        new OwnerInit(owner),
                        new LocationInit(location)
                    });
                    spawnedUnits.Add(actor);
                }
                unitsToSpawn--;
                if (unitsToSpawn <= 0)
                    internalWave++;
            }
        }

        public bool shouldSpawnActorForPlayer(string owner, string actorName)
        { 
            var p = world.Players.Where(player => player.InternalName == owner).Last();
            return (Rules.Info.ContainsKey(actorName.ToLowerInvariant()) && p.WinState == WinState.Undefined);
        }

        public void givePlayersCash()
        {
            for (int i = 0; i < currentWave().PlayersToGiveCash.Length; i++)
            {
                var player = world.Players.Where(p => p.InternalName == currentWave().PlayersToGiveCash[i]).Last();
                if (player != null)
                {
                    var amount = 0;
                    if (currentWave().PlayersToGiveCashAmount.Length > 0)
                    {
                        if (i < currentWave().PlayersToGiveCashAmount.Length)
                            amount = currentWave().PlayersToGiveCashAmount[i];
                        else
                            amount = currentWave().PlayersToGiveCashAmount[currentWave().PlayersToGiveCashAmount.Length - 1];
                    }
                    player.PlayerActor.Trait<PlayerResources>().GiveCash(amount);
                }
            }
        }

        public int nrUnitsAliveFromWave()
        {
            return spawnedUnits.Where(actor => !actor.IsDead()).Count();
        }

        public bool isWaveDone()
        {
            return nrUnitsAliveFromWave() <= 0;
        }

        public bool isActorsAlive()
        {
            var isAlive = true;
            if (Info.ActorsAlive != null)
            {
                var actors = world.WorldActor.Trait<SpawnMapActors>().Actors;
                var nrOfDeadActors = actors.Where(actor => Info.ActorsAlive.Contains(actor.Key) && actor.Value.IsDead()).Count();
                isAlive = nrOfDeadActors == 0;
            }
            return isAlive;
        }

        enum State { Complete = -2, Initial = -1, WaveStart = 0, WaveSpawn = 1 };
        State state = State.Initial;

        int unitsToSpawn = 0;
        List<Actor> spawnedUnits;

        int delay = 0;
        int internalWave = 0;
        int spawnRepeat = 0;
        int waveRepeat = 0;

        bool didBegin = false;

        public static Dictionary<string, object> master = new Dictionary<string, object>();
        bool isMaster = true;
        bool useMaster = false;

        int autoStart = -1;

        public void Tick(Actor self)
        {
            if (world == null)
            {
                world = self.World;
                if (Info.IsSynced)
                {
                    useMaster = true;
                    var type = self.Info.Name;
                    if (!master.ContainsKey(type))
                        master.Add(type, this);
                    else
                        isMaster = false;
                }
            }
            if (state == State.Initial)
            {
                delay = Info.Delay;
                state = State.WaveSpawn;
            }
            if (delay <= 0 && state == State.WaveSpawn)
            {
                if (Info.Waves.Length > currentWaveIndex)
                    startWave();
                else
                    if (spawnRepeat == 0)
                        completeSpawner();
                    else
                        repeatSpawn();
            }
            if (delay <= 0 && state == State.WaveStart)
            {
                if (!isActorsAlive()) {
                    completeSpawner();
                    return;
                }
                if (!didBegin)
                {
                    if (currentWave().MessageDidBegin.Length > 0 && isMaster)
                        Game.AddChatLine(System.Drawing.Color.Red, "Info", currentWave().MessageDidBegin);
                    didBegin = true;
                }
                if (currentWave().Units.Length > internalWave && unitsToSpawn <= 0)
                {
                    // New kind of units to spawn
                    var amounts = 1;
                    if (currentWave().Amounts.Length <= internalWave)
                        amounts = currentWave().Amounts[currentWave().Amounts.Length - 1];
                    else
                        amounts = currentWave().Amounts[internalWave];
                    unitsToSpawn = amounts;
                    spawnUnit(self.Location);
                    delay = currentWave().DelayBetweenSubSpawn;
                }
                else
                {
                    if (unitsToSpawn > 0)
                    {
                        if (currentWave().MaxUnits > nrUnitsAliveFromWave() || currentWave().MaxUnits == -1)
                        {
                            // Spawn units until there is no more for this unit type.
                            spawnUnit(self.Location);
                            delay = currentWave().DelayBetweenSubSpawn;
                        }
                        else
                            delay = 25;
                    }
                    else
                    {
                        var isDone = true;

                        // Check if all units are dead
                        if (currentWave().Clear)
                        {
                            if (useMaster)
                            {
                                var actors = world.ActorsWithTrait<Spawner>().ToArray();
                                for (int i = 0; i < actors.Length; i++)
                                {
                                    var s = actors[i].Trait;
                                    if (!s.isWaveDone())
                                    {
                                        isDone = false;
                                        delay = 25;
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                if (!isWaveDone())
                                {
                                    isDone = false;
                                    delay = 25;
                                }
                            }
                        }

                        if (isDone)
                        {
                            // Wave is complete
                            spawnedUnits.Clear();
                            if (waveRepeat == 0)
                            {
                                // No more units to spawn.
                                givePlayersCash();
                                if (currentWave().MessageEnded.Length > 0 && isMaster)
                                    Game.AddChatLine(System.Drawing.Color.Red, "Info", currentWave().MessageEnded);
                                completeWave();
                            }
                            else
                                repeatWave();
                        }
                    }
                }
            }
            delay--;

            // Automatically kill units after x time.
            if (autoStart > 0)
            {
                autoStart--;
                if (autoStart == 0)
                {
                    if (currentWave().AutoKill)
                        foreach (Actor actor in spawnedUnits)
                            actor.Destroy();
                    spawnedUnits.Clear();
                }
            }
        }
    }
}
