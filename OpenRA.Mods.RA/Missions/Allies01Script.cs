#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
    class Allies01ScriptInfo : TraitInfo<Allies01Script>, Requires<SpawnMapActorsInfo> { }

    class Allies01Script : IWorldLoaded, ITick
    {
        static readonly string[] objectives =
        {
            "Find Einstein.",
            "Wait for the helicopter and extract Einstein."
        };

        int currentObjective;

        Player allies;
        Player soviets;

        Actor insertionLZ;
        Actor extractionLZ;
        Actor lab;
        Actor insertionLZEntryPoint;
        Actor extractionLZEntryPoint;
        Actor chinookExitPoint;
        Actor shipSpawnPoint;
        Actor shipMovePoint;
        Actor einstein;
        Actor einsteinChinook;
        Actor tanya;
        Actor attackEntryPoint1;
        Actor attackEntryPoint2;

        static readonly string[] taunts = { "laugh1.aud", "lefty1.aud", "cmon1.aud", "gotit1.aud" };

        static readonly string[] ships = { "ca", "ca", "ca", "ca" };
        static readonly string[] patrol = { "e1", "dog", "e1" };

        static readonly string[] attackWave = { "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e2", "dog" };
        static readonly string[] lastAttackWaveAddition = { "3tnk", "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e2" };
        int currentAttackWaveFrameNumber;
        int currentAttackWave;
        const int einsteinChinookArrivesAtAttackWave = 5;

        const int labRange = 5;
        const string einsteinName = "einstein";
        const string tanyaName = "e7";
        const string chinookName = "tran";
        const string signalFlareName = "flare";

        void DisplayObjective()
        {
            Game.AddChatLine(Color.LimeGreen, "Objective", objectives[currentObjective]);
            Sound.Play("bleep6.aud", 5);
        }

        void MissionFailed(Actor self, string text)
        {
            if (allies.WinState != WinState.Undefined)
            {
                return;
            }
            allies.WinState = WinState.Lost;
            Game.AddChatLine(Color.Red, "Mission failed", text);
            self.World.LocalShroud.Disabled = true;
            Sound.Play("misnlst1.aud", 5);
        }

        void MissionAccomplished(Actor self, string text)
        {
            if (allies.WinState != WinState.Undefined)
            {
                return;
            }
            allies.WinState = WinState.Won;
            Game.AddChatLine(Color.Blue, "Mission accomplished", text);
            self.World.LocalShroud.Disabled = true;
            Sound.Play("misnwon1.aud", 5);
        }

        public void Tick(Actor self)
        {
            if (allies.WinState != WinState.Undefined)
            {
                return;
            }
            // display current objective every so often
            if (self.World.FrameNumber % 1500 == 1)
            {
                DisplayObjective();
            }
            // taunt every so often
            if (self.World.FrameNumber % 1000 == 0)
            {
                Sound.Play(taunts[self.World.SharedRandom.Next(taunts.Length)]);
            }
            // take Tanya to the LZ
            if (self.World.FrameNumber == 1)
            {
                FlyTanyaToInsertionLZ(self);
                SendPatrol(self);
            }
            // objectives
            if (currentObjective == 0)
            {
                if (AlliesControlLab(self))
                {
                    SpawnSignalFlare(self);
                    Sound.Play("flaren1.aud", 5);
                    SpawnEinsteinAtLab(self); // spawn Einstein once the area is clear
                    Sound.Play("einok1.aud"); // "Incredible!" - Einstein
                    SendShips(self);
                    currentObjective++;
                    DisplayObjective();
                    currentAttackWaveFrameNumber = self.World.FrameNumber;
                }
                if (lab.Destroyed)
                {
                    MissionFailed(self, "Einstein was killed.");
                }
            }
            else if (currentObjective == 1)
            {
                if (self.World.FrameNumber >= currentAttackWaveFrameNumber + 600)
                {
                    Sound.Play("enmyapp1.aud", 5);
                    SendAttackWave(self, attackWave);
                    currentAttackWave++;
                    currentAttackWaveFrameNumber = self.World.FrameNumber;
                    if (currentAttackWave >= einsteinChinookArrivesAtAttackWave)
                    {
                        SendAttackWave(self, lastAttackWaveAddition);
                    }
                    if (currentAttackWave == einsteinChinookArrivesAtAttackWave)
                    {
                        FlyEinsteinFromExtractionLZ(self);
                    }
                }
                if (einsteinChinook != null && !self.World.Map.IsInMap(einsteinChinook.Location) && einsteinChinook.Trait<Cargo>().Passengers.Contains(einstein))
                {
                    MissionAccomplished(self, "Einstein was rescued.");
                }
                if (einstein.Destroyed)
                {
                    MissionFailed(self, "Einstein was killed.");
                }
            }
            if (tanya.Destroyed)
            {
                MissionFailed(self, "Tanya was killed.");
            }
            ManageSovietOre();
        }

        void ManageSovietOre()
        {
            var res = soviets.PlayerActor.Trait<PlayerResources>();
            res.TakeOre(res.Ore);
            res.TakeCash(res.Cash);
        }

        void SpawnSignalFlare(Actor self)
        {
            self.World.CreateActor(signalFlareName, new TypeDictionary { new OwnerInit(allies), new LocationInit(extractionLZ.Location) });
        }

        void SendAttackWave(Actor self, IEnumerable<string> wave)
        {
            foreach (var unit in wave)
            {
                var spawnActor = self.World.SharedRandom.Next(2) == 0 ? attackEntryPoint1 : attackEntryPoint2;
                var actor = self.World.CreateActor(unit, new TypeDictionary { new OwnerInit(soviets), new LocationInit(spawnActor.Location) });
                Activity innerActivity;
                if (einstein != null && einstein.IsInWorld)
                {
                    innerActivity = new Attack(Target.FromActor(einstein), 3);
                }
                else
                {
                    innerActivity = new Move.Move(extractionLZ.Location, 3);
                }
                actor.QueueActivity(new AttackMove.AttackMoveActivity(actor, innerActivity));
            }
        }

        void SendPatrol(Actor self)
        {
            for (int i = 0; i < patrol.Length; i++)
            {
                var actor = self.World.CreateActor(patrol[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(insertionLZ.Location + new CVec(-1 + i, 10 + i * 2)) });
                actor.QueueActivity(new Move.Move(insertionLZ.Location));
            }
        }

        IEnumerable<Actor> UnitsNearActor(Actor self, Actor actor, int range)
        {
            return self.World.FindUnitsInCircle(actor.CenterLocation, Game.CellSize * range)
                .Where(a => a.IsInWorld && a != self.World.WorldActor && !a.Destroyed && a.HasTrait<IMove>() && !a.Owner.NonCombatant);
        }

        bool AlliesControlLab(Actor self)
        {
            var units = UnitsNearActor(self, lab, labRange);
            return units.Any() && units.All(a => a.Owner == allies);
        }

        void SpawnEinsteinAtLab(Actor self)
        {
            einstein = self.World.CreateActor(einsteinName, new TypeDictionary { new OwnerInit(allies), new LocationInit(lab.Location) });
            einstein.QueueActivity(new Move.Move(lab.Location - new CVec(0, 2)));
        }

        void SendShips(Actor self)
        {
            for (int i = 0; i < ships.Length; i++)
            {
                var actor = self.World.CreateActor(ships[i],
                    new TypeDictionary { new OwnerInit(allies), new LocationInit(shipSpawnPoint.Location + new CVec(i * 2, 0)) });
                actor.QueueActivity(new Move.Move(shipMovePoint.Location + new CVec(i * 4, 0)));
            }
        }

        void FlyEinsteinFromExtractionLZ(Actor self)
        {
            einsteinChinook = self.World.CreateActor(chinookName, new TypeDictionary { new OwnerInit(allies), new LocationInit(extractionLZEntryPoint.Location) });
            einsteinChinook.QueueActivity(new HeliFly(extractionLZ.CenterLocation));
            einsteinChinook.QueueActivity(new Turn(0));
            einsteinChinook.QueueActivity(new HeliLand(true));
            einsteinChinook.QueueActivity(new WaitFor(() => einsteinChinook.Trait<Cargo>().Passengers.Contains(einstein)));
            einsteinChinook.QueueActivity(new Wait(150));
            einsteinChinook.QueueActivity(new HeliFly(chinookExitPoint.CenterLocation));
            einsteinChinook.QueueActivity(new RemoveSelf());
        }

        void FlyTanyaToInsertionLZ(Actor self)
        {
            tanya = self.World.CreateActor(false, tanyaName, new TypeDictionary { new OwnerInit(allies) });
            var chinook = self.World.CreateActor(chinookName, new TypeDictionary { new OwnerInit(allies), new LocationInit(insertionLZEntryPoint.Location) });
            chinook.Trait<Cargo>().Load(chinook, tanya);
            chinook.QueueActivity(new HeliFly(insertionLZ.CenterLocation));
            chinook.QueueActivity(new Turn(0));
            chinook.QueueActivity(new HeliLand(true));
            chinook.QueueActivity(new UnloadCargo(true));
            chinook.QueueActivity(new CallFunc(() => Sound.Play("laugh1.aud")));
            chinook.QueueActivity(new Wait(150));
            chinook.QueueActivity(new HeliFly(chinookExitPoint.CenterLocation));
            chinook.QueueActivity(new RemoveSelf());
        }

        public void WorldLoaded(World w)
        {
            allies = w.Players.Single(p => p.InternalName == "Allies");
            soviets = w.Players.Single(p => p.InternalName == "Soviets");
            var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
            insertionLZ = actors["InsertionLZ"];
            extractionLZ = actors["ExtractionLZ"];
            lab = actors["Lab"];
            insertionLZEntryPoint = actors["InsertionLZEntryPoint"];
            chinookExitPoint = actors["ChinookExitPoint"];
            extractionLZEntryPoint = actors["ExtractionLZEntryPoint"];
            shipSpawnPoint = actors["ShipSpawnPoint"];
            shipMovePoint = actors["ShipMovePoint"];
            attackEntryPoint1 = actors["SovietAttackEntryPoint1"];
            attackEntryPoint2 = actors["SovietAttackEntryPoint2"];
            Game.MoveViewport(insertionLZ.Location.ToFloat2());
            Sound.PlayMusic(Rules.Music["hell226m"]); // Hell March
            Game.ConnectionStateChanged += StopMusic;
        }

        void StopMusic(OrderManager orderManager)
        {
            if (!orderManager.GameStarted)
            {
                Sound.StopMusic();
                Game.ConnectionStateChanged -= StopMusic;
            }
        }
    }
}
