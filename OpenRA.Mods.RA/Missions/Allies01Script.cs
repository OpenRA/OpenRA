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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
    public class Allies01ScriptInfo : TraitInfo<Allies01Script>, Requires<SpawnMapActorsInfo> { }

    public class Allies01Script : IWorldLoaded, ITick
    {
        private static readonly string[] objectives = 
		{
			"Find Einstein.",
			"Wait for the helicopter and extract Einstein."
		};

        private int currentObjective;

        private Player allies;
        private Player soviets;

        private ISound music;

        private Actor insertionLZ;
        private Actor extractionLZ;
        private Actor lab;
        private Actor insertionLZEntryPoint;
        private Actor extractionLZEntryPoint;
        private Actor chinookExitPoint;
        private Actor shipSpawnPoint;
        private Actor shipMovePoint;
        private Actor einstein;
        private Actor einsteinChinook;
        private Actor tanya;
        private Actor attackEntryPoint1;
        private Actor attackEntryPoint2;

        private static readonly string[] taunts = { "laugh1.aud", "lefty1.aud", "cmon1.aud", "gotit1.aud" };

        private static readonly string[] ships = { "ca", "ca", "ca", "ca" };

        private static readonly string[] attackWave = { "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e2", "dog" };
        private int currentAttackWaveFrameNumber;
        private int currentAttackWave;
        private const int einsteinChinookArrivesAtAttackWave = 5;

        private const int labRange = 5;
        private const string einsteinName = "c1";
        private const string tanyaName = "e7";
        private const string chinookName = "tran";
        private const string signalFlareName = "flare";

        private void NextObjective()
        {
            currentObjective++;
        }

        private void DisplayObjective()
        {
            Game.AddChatLine(Color.LimeGreen, "Objective", objectives[currentObjective]);
            Sound.Play("bleep6.aud", 5);
        }

        private void MissionFailed(Actor self, string text)
        {
            if (allies.WinState != WinState.Undefined)
            {
                return;
            }
            allies.WinState = WinState.Lost;
            Game.AddChatLine(Color.Red, "Mission failed", text);
            foreach (var actor in self.World.Actors.Where(a => a.Owner == allies))
            {
                actor.Kill(actor);
            }
            self.World.LocalShroud.Disabled = true;
            Sound.Play("misnlst1.aud", 5);
        }

        private void MissionAccomplished(Actor self, string text)
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
                    NextObjective();
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
                    if (currentAttackWave == einsteinChinookArrivesAtAttackWave)
                    {
                        FlyEinsteinFromExtractionLZ(self);
                    }
                }
                if (einsteinChinook != null && !self.World.Map.IsInMap(einsteinChinook.Location) && !einstein.IsInWorld)
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
        }

        private void SpawnSignalFlare(Actor self)
        {
            self.World.CreateActor(signalFlareName, new TypeDictionary { new OwnerInit(allies), new LocationInit(extractionLZ.Location) });
        }

        private void SendAttackWave(Actor self, IEnumerable<string> wave)
        {
            foreach (var unit in wave)
            {
                var spawnActor = self.World.SharedRandom.Next(2) == 0 ? attackEntryPoint1 : attackEntryPoint2;
                var actor = self.World.CreateActor(unit, new TypeDictionary { new OwnerInit(soviets), new LocationInit(spawnActor.Location) });
                actor.QueueActivity(new AttackMove.AttackMoveActivity(actor, new Attack(Target.FromActor(einstein), 3))); // better way of doing this?
            }
        }

        private IEnumerable<Actor> UnitsNearActor(Actor self, Actor actor, int range)
        {
            return self.World.FindUnitsInCircle(actor.CenterLocation, Game.CellSize * range)
                .Where(a => a.IsInWorld && a != self.World.WorldActor && !a.Destroyed && a.HasTrait<IMove>() && !a.Owner.NonCombatant);
        }

        private bool AlliesControlLab(Actor self)
        {
            var units = UnitsNearActor(self, lab, labRange);
            return units.Any() && units.All(a => a.Owner == allies);
        }

        private void SpawnEinsteinAtLab(Actor self)
        {
            einstein = self.World.CreateActor(einsteinName, new TypeDictionary { new OwnerInit(allies), new LocationInit(lab.Location) });
        }

        private void SendShips(Actor self)
        {
            for (int i = 0; i < ships.Length; i++)
            {
                var actor = self.World.CreateActor(ships[i],
                    new TypeDictionary { new OwnerInit(allies), new LocationInit(shipSpawnPoint.Location + new int2(i * 2, 0)) });
                actor.QueueActivity(new Move.Move(shipMovePoint.Location + new int2(i * 4, 0)));
            }
        }

        private void FlyEinsteinFromExtractionLZ(Actor self)
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

        private void FlyTanyaToInsertionLZ(Actor self)
        {
            tanya = self.World.CreateActor(false, tanyaName, new TypeDictionary { new OwnerInit(allies) });
            var chinook = self.World.CreateActor(chinookName, new TypeDictionary { new OwnerInit(allies), new LocationInit(insertionLZEntryPoint.Location) });
            chinook.Trait<Cargo>().Load(chinook, tanya);
            // use CenterLocation for HeliFly, Location for Move
            chinook.QueueActivity(new HeliFly(insertionLZ.CenterLocation));
            chinook.QueueActivity(new Turn(0));
            chinook.QueueActivity(new HeliLand(true));
            chinook.QueueActivity(new UnloadCargo());
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
            music = Sound.Play("hell226m.aud"); // Hell March
            Game.ConnectionStateChanged += StopMusic;
        }

        private void StopMusic(OrderManager orderManager)
        {
            if (!orderManager.GameStarted)
            {
                Sound.StopSound(music);
                Game.ConnectionStateChanged -= StopMusic;
            }
        }
    }
}
