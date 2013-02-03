using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Move;
using OpenRA.Network;
using OpenRA.Scripting;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Effects;

namespace OpenRA.Mods.RA.Missions
{
    class Survival01ScriptInfo : TraitInfo<Survival01Script>, Requires<SpawnMapActorsInfo> { }

    class Survival01Script : IHasObjectives, IWorldLoaded, ITick
    {
        public event ObjectivesUpdatedEventHandler OnObjectivesUpdated = notify => { };

        public IEnumerable<Objective> Objectives { get { return objectives.Values; } }

        Dictionary<int, Objective> objectives = new Dictionary<int, Objective>
		{
			{ MaintainPresenceID, new Objective(ObjectiveType.Primary, MaintainPresence, ObjectiveStatus.InProgress) },
            { DestroySovietsID, new Objective(ObjectiveType.Primary, DestroySoviets, ObjectiveStatus.Inactive) }
		};

        const int MaintainPresenceID = 0;
        const int DestroySovietsID = 1;

        const string MaintainPresence = "Enforce your position and hold-out the onslaught until reinforcements arrive. We must not lose the base!";
        const string DestroySoviets = "Take control of french reinforcements and dismantle the nearby Soviet base.";

        Player allies;
        Player soviets;

        Actor sovietEntryPoint1;
        Actor sovietEntryPoint2;
        Actor sovietEntryPoint3;
        Actor sovietEntryPoint4;
        Actor sovietEntryPoint5;
        CPos[] sovietEntryPoints;
        Actor sovietRallyPoint1;
        Actor sovietRallyPoint2;
        Actor sovietRallyPoint3;
        Actor sovietRallyPoint4;
        Actor sovietRallyPoint5;
        CPos[] sovietRallyPoints;

        Actor sovietinfantryentry1;
        Actor sovietinfantryentry2;
        CPos[] sovietEntryPointInf;
        Actor sovietinfantryrally1;
        Actor sovietinfantryrally2;
        CPos[] sovietRallyPointInf;

        Actor BadgerEntryPoint1;
        Actor BadgerEntryPoint2;
        Actor ParaDrop1;
        Actor ParaDrop2;
        Actor ParaBomb1;
        Actor ParaBomb2;
        Actor sovietEntryPoint7;

        Actor alliesbase1;
        Actor alliesbase2;
        Actor alliesbase3;
        Actor alliesbase;
        Actor alliesEntryPoint;
        Actor sam1;
        Actor sam2;
        Actor barrack1;
        Actor factory;
        World world;

        CountdownTimer CountDownTimer;
        CountdownTimerWidget CountDownTimerWidget;

        int attackAtFrame;
        int attackAtFrameIncrement;
        int attackAtFrameInf;
        int attackAtFrameIncrementInf;

        const int ParadropTicks = 750;
        static readonly string[] Badger1Passengers = { "e1", "e1", "e1", "e2", "e2" };
        static readonly string[] Badger2Passengers = { "e1", "e1", "e1", "e2", "e2" };
        static readonly string[] Badger3Passengers = { "e1", "e1", "e4", "e3", "e3" };

        const int FactoryClearRange = 10;
        static readonly string[] Squad1 = { "e1", "e1" };
        static readonly string[] Squad2 = { "e2", "e2" };
        static readonly string[] SovietVehicles = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "ftrk", "ftrk", "ftrk", "apc", "apc" };
        static readonly string[] SovietInfantry = { "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e4", "e4", "e3", };
        static readonly string[] Reinforcements = { "2tnk", "2tnk", "2tnk", "2tnk", "2tnk", "1tnk", "1tnk", "1tnk", "arty", "arty", "arty", "jeep", "jeep" };
        const int SovietAttackGroupSize = 5;
        const int SovietInfantryGroupSize = 10;

        const int TimerTicks = 1500 * 25;

        void MissionAccomplished(string text)
        {
            if (allies.WinState != WinState.Undefined)
            {
                return;
            }
            allies.WinState = WinState.Won;
            Game.AddChatLine(Color.Blue, "Mission accomplished", text);
            Sound.Play("misnwon1.aud");
        }

        void SendSquad1()
        {
            for (int i = 0; i < Squad1.Length; i++)
            {
                var actor = world.CreateActor(Squad1[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(alliesbase1.Location + new CVec(-2 + i, -6 + i * 2)) });
                actor.QueueActivity(new Move.Move(alliesbase1.Location + new CVec(-2, -5)));
            }
        }

        void SendSquad2()
        {
            for (int i = 0; i < Squad2.Length; i++)
            {
                var actor = world.CreateActor(Squad2[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(alliesbase2.Location + new CVec(-9 + i, -2 + i * 2)) });
                actor.QueueActivity(new Move.Move(alliesbase2.Location + new CVec(-3, -1)));
            }
        }

        public void Tick(Actor self)
        {
            CountDownTimer.Tick();
            if (allies.WinState != WinState.Undefined)
            {
                return;
            }
            if (world.FrameNumber == attackAtFrame)
            {
                attackAtFrame += attackAtFrameIncrement;
                attackAtFrameIncrement = Math.Max(attackAtFrameIncrement - 5, 100);
                SpawnSovietUnits();
                ManageSovietUnits();
                ManageSovietOre();
            }
            if (world.FrameNumber == attackAtFrameInf)
            {
                attackAtFrameInf += attackAtFrameIncrementInf;
                attackAtFrameIncrementInf = Math.Max(attackAtFrameIncrementInf - 5, 100);
                SpawnSovietInfantry();
                ManageSovietInfantry();
            }
            if (barrack1.Destroyed)
            {
                SpawningInfantry = false;
            }
            if (world.FrameNumber == ParadropTicks)
            {
                MissionUtils.Paradrop(world, soviets, Badger1Passengers, BadgerEntryPoint1.Location, ParaDrop1.Location);
                MissionUtils.Paradrop(world, soviets, Badger2Passengers, BadgerEntryPoint2.Location, ParaDrop2.Location);
            }
            if (world.FrameNumber == ParadropTicks * 2)
                {
                    {
                        MissionUtils.Paradrop(world, soviets, Badger3Passengers, BadgerEntryPoint1.Location, alliesbase2.Location);
                        MissionUtils.Paradrop(world, soviets, Badger3Passengers, BadgerEntryPoint2.Location, alliesbase1.Location);
                    }
                }
            if (world.FrameNumber == 1500 * 23)
            {
                attackAtFrame = 100;
                attackAtFrameIncrement = 100;
            }
            if (world.FrameNumber == 1500 * 25)
            {
                SpawningSovietUnits = false;
            }
            if (objectives[DestroySovietsID].Status == ObjectiveStatus.InProgress)
            {
                if (barrack1.Destroyed)
                {
                    objectives[DestroySovietsID].Status = ObjectiveStatus.Completed;
                    OnObjectivesUpdated(true);
                    MissionAccomplished("The French forces have survived and dismantled the soviet presence in the area!");
                }
            }
        }

        void SpawnSovietInfantry()
        {
            if (SpawningInfantry == true)
            {
                var route = world.SharedRandom.Next(sovietEntryPointInf.Length);
                var spawnPoint = sovietEntryPointInf[route];
                var rallyPoint = sovietRallyPointInf[route];
                var inf = world.CreateActor(SovietInfantry.Random(world.SharedRandom), new TypeDictionary { new LocationInit(spawnPoint), new OwnerInit(soviets) });
                inf.QueueActivity(new AttackMove.AttackMoveActivity(inf, new Move.Move(rallyPoint, 3)));
            }
        }

        void SpawnSovietUnits()
        {
            if (SpawningSovietUnits == true)
            {
                var route = world.SharedRandom.Next(sovietEntryPoints.Length);
                var spawnPoint = sovietEntryPoints[route];
                var rallyPoint = sovietRallyPoints[route];
                var unit = world.CreateActor(SovietVehicles.Random(world.SharedRandom),
                    new TypeDictionary { new LocationInit(spawnPoint), new OwnerInit(soviets) });
                unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, new Move.Move(rallyPoint, 3)));
            }
        }

        bool SpawningSovietUnits = true;

        bool SpawningInfantry = true;

        void AttackNearestAlliedActor(Actor self)
        {
            var enemies = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && (u.Owner == allies)
                && ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || u.HasTrait<Mobile>()));
            var targetEnemy = enemies.OrderBy(u => (self.CenterLocation - u.CenterLocation).LengthSquared).FirstOrDefault();
            if (targetEnemy != null)
            {
                self.QueueActivity(new AttackMove.AttackMoveActivity(self, new Attack(Target.FromActor(targetEnemy), 3)));
            }
        }

        void ManageSovietOre()
        {
            var res = soviets.PlayerActor.Trait<PlayerResources>();
            res.TakeOre(res.Ore);
            res.TakeCash(res.Cash);
        }

        void ManageSovietInfantry()
        {
            foreach (var rallyPoint in sovietRallyPointInf)
            {
                var infs = world.FindAliveCombatantActorsInCircle(Util.CenterOfCell(rallyPoint), 4)
                    .Where(u => u.IsIdle && u.HasTrait<Mobile>() && u.Owner == soviets);
                if (infs.Count() >= SovietInfantryGroupSize)
                {
                    foreach (var inf in infs)
                    {
                        AttackNearestAlliedActor(inf);
                    }
                }
            }
            var scatteredInfs = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && u.HasTrait<Mobile>() && u.IsIdle && u.Owner == soviets)
                .Except(world.WorldActor.Trait<SpawnMapActors>().Actors.Values)
                .Except(sovietRallyPoints.SelectMany(rp => world.FindAliveCombatantActorsInCircle(Util.CenterOfCell(rp), 4)));
            foreach (var inf in scatteredInfs)
            {
                AttackNearestAlliedActor(inf);
            }
        }

        void ManageSovietUnits()
        {
            foreach (var rallyPoint in sovietRallyPoints)
            {
                var units = world.FindAliveCombatantActorsInCircle(Util.CenterOfCell(rallyPoint), 4)
                    .Where(u => u.IsIdle && u.HasTrait<Mobile>() && u.Owner == soviets);
                if (units.Count() >= SovietAttackGroupSize)
                {
                    foreach (var unit in units)
                    {
                        AttackNearestAlliedActor(unit);
                    }
                }
            }
            var scatteredUnits = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && u.HasTrait<Mobile>() && u.IsIdle && u.Owner == soviets)
                .Except(world.WorldActor.Trait<SpawnMapActors>().Actors.Values)
                .Except(sovietRallyPoints.SelectMany(rp => world.FindAliveCombatantActorsInCircle(Util.CenterOfCell(rp), 4)));
            foreach (var unit in scatteredUnits)
            {
                AttackNearestAlliedActor(unit);
            }
        }

        void StartCountDownTimer()
        {
            Sound.Play("timergo1.aud");
            CountDownTimer = new CountdownTimer(TimerTicks, CountDownTimerExpired, true);
            CountDownTimerWidget = new CountdownTimerWidget(
                CountDownTimer,
                "Survive: {0}",
                new float2(Game.viewport.Width * 0.5f, Game.viewport.Height * 0.9f));
            Ui.Root.AddChild(CountDownTimerWidget);
        }

        void SendReinforcements()
        {
            foreach (var unit in Reinforcements)
            {
                var u = world.CreateActor(unit, new TypeDictionary
				{
					new LocationInit(sovietEntryPoint7.Location),
					new FacingInit(0),
					new OwnerInit(allies)
				});
                u.QueueActivity(new Move.Move(alliesbase.Location));
            }
        }

        void CountDownTimerExpired(CountdownTimer CountDownTimer)
        {
            CountDownTimerWidget.Visible = false;
            SendReinforcements();
            objectives[MaintainPresenceID].Status = ObjectiveStatus.Completed;
            objectives[DestroySovietsID].Status = ObjectiveStatus.InProgress;
            OnObjectivesUpdated(true);
        }

        public void WorldLoaded(World w)
        {
            world = w;
            allies = w.Players.Single(p => p.InternalName == "Allies");
            if (allies != null)
            {
                attackAtFrameInf = 300;
                attackAtFrameIncrementInf = 300;
                attackAtFrame = 450;
                attackAtFrameIncrement = 450;
            }
            soviets = w.Players.Single(p => p.InternalName == "Soviets");
            var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
            sovietEntryPoint1 = actors["sovietEntryPoint1"];
            sovietEntryPoint2 = actors["sovietEntryPoint2"];
            sovietEntryPoint3 = actors["sovietEntryPoint3"];
            sovietEntryPoint4 = actors["sovietEntryPoint4"];
            sovietEntryPoint5 = actors["sovietEntryPoint5"];
            sovietEntryPoints = new[] { sovietEntryPoint1, sovietEntryPoint2, sovietEntryPoint3, sovietEntryPoint4, sovietEntryPoint5 }.Select(p => p.Location).ToArray();
            sovietRallyPoint1 = actors["sovietRallyPoint1"];
            sovietRallyPoint2 = actors["sovietRallyPoint2"];
            sovietRallyPoint3 = actors["sovietRallyPoint3"];
            sovietRallyPoint4 = actors["sovietRallyPoint4"];
            sovietRallyPoint5 = actors["sovietRallyPoint5"];
            sovietRallyPoints = new[] { sovietRallyPoint1, sovietRallyPoint2, sovietRallyPoint3, sovietRallyPoint4, sovietRallyPoint5 }.Select(p => p.Location).ToArray();
            alliesbase = actors["alliesbase"];
            alliesbase1 = actors["alliesbase1"];
            alliesbase2 = actors["alliesbase2"];
            alliesbase3 = actors["alliesbase3"];
            alliesEntryPoint = actors["alliesEntryPoint"];
            BadgerEntryPoint1 = actors["BadgerEntryPoint1"];
            BadgerEntryPoint2 = actors["BadgerEntryPoint2"];
            sovietEntryPoint7 = actors["sovietEntryPoint7"];
            sovietinfantryentry1 = actors["SovietInfantryEntry1"];
            sovietinfantryentry2 = actors["SovietInfantryEntry2"];
            sovietEntryPointInf = new[] { sovietinfantryentry1, sovietinfantryentry2 }.Select(p => p.Location).ToArray();
            sovietinfantryrally1 = actors["SovietInfantryRally1"];
            sovietinfantryrally2 = actors["SovietInfantryRally2"];
            sovietRallyPointInf = new[] { sovietinfantryrally1, sovietinfantryrally2 }.Select(p => p.Location).ToArray();
            ParaDrop1 = actors["ParaDrop1"];
            ParaDrop2 = actors["ParaDrop2"];
            ParaBomb1 = actors["ParaBomb1"];
            ParaBomb2 = actors["ParaBomb2"];
            barrack1 = actors["Barrack1"];
            sam1 = actors["Sam1"];
            sam2 = actors["Sam2"];
            factory = actors["Factory"];
            var shroud = w.WorldActor.Trait<Shroud>();
            shroud.Explore(w, sam1.Location, 4);
            shroud.Explore(w, sam2.Location, 4);
            Game.MoveViewport(alliesbase.Location.ToFloat2());
            StartCountDownTimer();
            SendSquad1();
            SendSquad2();
        }
    }
}
