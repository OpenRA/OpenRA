#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Missions
{
	class Survival01ScriptInfo : TraitInfo<Survival01Script>, Requires<SpawnMapActorsInfo> { }

	class Survival01Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event Action<bool> OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return new[] { maintainPresence, destroySoviets }; } }

		Objective maintainPresence = new Objective(ObjectiveType.Primary, MaintainPresenceText, ObjectiveStatus.InProgress);
		Objective destroySoviets = new Objective(ObjectiveType.Primary, DestroySovietsText, ObjectiveStatus.Inactive);

		const string MaintainPresenceText = "Enforce your position and hold-out the onslaught until reinforcements arrive. We must not lose the base!";
		const string DestroySovietsText = "Take control of French reinforcements and dismantle the nearby Soviet base.";

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
		Actor sovietinfantryrally1;

		Actor badgerEntryPoint1;
		Actor badgerEntryPoint2;
		Actor paraDrop1;
		Actor paraDrop2;
		Actor sovietEntryPoint7;

		Actor alliesbase1;
		Actor alliesbase2;
		Actor alliesbase;
		Actor sam1;
		Actor sam2;
		Actor barrack1;
		World world;

		CountdownTimer survivalTimer;
		CountdownTimerWidget survivalTimerWidget;

		int attackAtFrame;
		int attackAtFrameIncrement;
		int attackAtFrameInf;
		int attackAtFrameIncrementInf;

		const int paradropTicks = 750;
		static readonly string[] badger1Passengers = { "e1", "e1", "e1", "e2", "e2" };

		const int factoryClearRange = 10;
		static readonly string[] squad1 = { "e1", "e1" };
		static readonly string[] squad2 = { "e2", "e2" };
		static readonly string[] sovietVehicles = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "ftrk", "ftrk", "ftrk", "apc", "apc" };
		static readonly string[] sovietInfantry = { "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e4", "e4", "e3", };
		static readonly string[] reinforcements = { "2tnk", "2tnk", "2tnk", "2tnk", "2tnk", "1tnk", "1tnk", "1tnk", "arty", "arty", "arty", "jeep", "jeep" };
		const int sovietAttackGroupSize = 5;
		const int sovietInfantryGroupSize = 7;

		const int timerTicks = 1500 * 25;
		bool spawningSovietUnits = true;
		bool spawningInfantry = true;
		string difficulty;

		void MissionAccomplished(string text)
		{
			MissionUtils.CoopMissionAccomplished(world, text, allies);
		}

		public void Tick(Actor self)
		{
			if (allies.WinState != WinState.Undefined)
				return;

			survivalTimer.Tick();

			if (world.FrameNumber == attackAtFrame)
			{
				attackAtFrame += attackAtFrameIncrement;
				attackAtFrameIncrement = Math.Max(attackAtFrameIncrement - 5, 100);
				SpawnSovietUnits();
				ManageSovietUnits();
				MissionUtils.CapOre(soviets);
			}

			if (world.FrameNumber == attackAtFrameInf)
			{
				attackAtFrameInf += attackAtFrameIncrementInf;
				attackAtFrameIncrementInf = Math.Max(attackAtFrameIncrementInf - 5, 100);
				SpawnSovietInfantry();
			}

			if (barrack1.Destroyed)
			{
				spawningInfantry = false;
			}

			if (world.FrameNumber == paradropTicks)
			{
				MissionUtils.Paradrop(world, soviets, badger1Passengers, badgerEntryPoint1.Location, paraDrop1.Location);
				MissionUtils.Paradrop(world, soviets, badger1Passengers, badgerEntryPoint2.Location, paraDrop2.Location);
			}

			if (world.FrameNumber == paradropTicks * 2)
			{
				MissionUtils.Paradrop(world, soviets, badger1Passengers, badgerEntryPoint1.Location, alliesbase2.Location);
				MissionUtils.Paradrop(world, soviets, badger1Passengers, badgerEntryPoint2.Location, alliesbase1.Location);
			}

			if (world.FrameNumber == 1500 * 23)
			{
				attackAtFrame = 100;
				attackAtFrameIncrement = 100;
			}

			if (world.FrameNumber == 1500 * 25)
			{
				spawningSovietUnits = false;
				spawningInfantry = false;
			}

			if (destroySoviets.Status == ObjectiveStatus.InProgress)
			{
				if (barrack1.Destroyed)
				{
					destroySoviets.Status = ObjectiveStatus.Completed;
					OnObjectivesUpdated(true);
					MissionAccomplished("The French forces have survived and dismantled the soviet presence in the area!");
				}
			}
		}

		void SendSquad1()
		{
			for (int i = 0; i < squad1.Length; i++)
			{
				var actor = world.CreateActor(squad1[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(alliesbase1.Location + new CVec(-2 + i, -6 + i * 2)) });
				actor.QueueActivity(new Move.Move(alliesbase1.Location));
			}
		}

		void SendSquad2()
		{
			for (int i = 0; i < squad2.Length; i++)
			{
				var actor = world.CreateActor(squad2[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(alliesbase2.Location + new CVec(-9 + i, -2 + i * 2)) });
				actor.QueueActivity(new Move.Move(alliesbase2.Location));
			}
		}

		void SpawnSovietInfantry()
		{
			if (spawningInfantry)
			{
				var units = world.CreateActor((sovietInfantry).Random(world.SharedRandom), new TypeDictionary { new LocationInit(sovietinfantryentry1.Location), new OwnerInit(soviets) });
				units.QueueActivity(new Move.Move(sovietinfantryrally1.Location, 3));
				var unitsincircle = world.FindAliveCombatantActorsInCircle(sovietinfantryrally1.CenterPosition, WDist.FromCells(10))
					.Where(a => a.Owner == soviets && a.IsIdle && a.HasTrait<IPositionable>());
				if (unitsincircle.Count() >= sovietInfantryGroupSize)
				{
					foreach (var scatteredunits in unitsincircle)
						AttackNearestAlliedActor(scatteredunits);
				}
			}
		}

		void SpawnSovietUnits()
		{
			if (spawningSovietUnits)
			{
				var route = world.SharedRandom.Next(sovietEntryPoints.Length);
				var spawnPoint = sovietEntryPoints[route];
				var rallyPoint = sovietRallyPoints[route];
				var unit = world.CreateActor(sovietVehicles.Random(world.SharedRandom),
					new TypeDictionary { new LocationInit(spawnPoint), new OwnerInit(soviets) });
				unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, new Move.Move(rallyPoint, 3)));
			}
		}

		void AttackNearestAlliedActor(Actor self)
		{
			var enemies = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && (u.Owner == allies)
				&& ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || u.HasTrait<Mobile>()));

			var targetEnemy = enemies.ClosestTo(self);
			if (targetEnemy != null)
				self.QueueActivity(new AttackMove.AttackMoveActivity(self, new Attack(Target.FromActor(targetEnemy), WDist.FromCells(3))));
		}

		void ManageSovietUnits()
		{
			foreach (var rallyPoint in sovietRallyPoints)
			{
				var units = world.FindAliveCombatantActorsInCircle(rallyPoint.CenterPosition, WDist.FromCells(4))
					.Where(u => u.IsIdle && u.HasTrait<Mobile>() && u.Owner == soviets);
				if (units.Count() >= sovietAttackGroupSize)
				{
					foreach (var unit in units)
					{
						AttackNearestAlliedActor(unit);
					}
				}
			}
			var scatteredUnits = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && u.HasTrait<Mobile>() && u.IsIdle && u.Owner == soviets)
				.Except(world.WorldActor.Trait<SpawnMapActors>().Actors.Values)
				.Except(sovietRallyPoints.SelectMany(rp => world.FindAliveCombatantActorsInCircle(rp.CenterPosition, WDist.FromCells(4))));
			foreach (var unit in scatteredUnits)
			{
				AttackNearestAlliedActor(unit);
			}
		}

		void StartCountDownTimer()
		{
			Sound.Play("timergo1.aud");
			survivalTimer = new CountdownTimer(timerTicks, CountDownTimerExpired, true);
			survivalTimerWidget = new CountdownTimerWidget(survivalTimer, "Survive: {0}");
			Ui.Root.AddChild(survivalTimerWidget);
		}

		void SendReinforcements()
		{
			foreach (var unit in reinforcements)
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

		void CountDownTimerExpired(CountdownTimer countDownTimer)
		{
			survivalTimerWidget.Visible = false;
			SendReinforcements();
			maintainPresence.Status = ObjectiveStatus.Completed;
			destroySoviets.Status = ObjectiveStatus.InProgress;
			OnObjectivesUpdated(true);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;

			allies = w.Players.SingleOrDefault(p => p.InternalName == "Allies");
			if (allies != null)
			{
				attackAtFrameInf = 300;
				attackAtFrameIncrementInf = 300;
				attackAtFrame = 450;
				attackAtFrameIncrement = 450;
			}

			difficulty = w.LobbyInfo.GlobalSettings.Difficulty;
			Game.Debug("{0} difficulty selected".F(difficulty));

			switch (difficulty)
			{
			case "Hard":
				attackAtFrameIncrement = 350;
				attackAtFrameIncrementInf = 200;
				break;
			case "Normal":
				attackAtFrameIncrement = 450;
				attackAtFrameIncrementInf = 300;
				break;
			case "Easy":
				attackAtFrameIncrement = 550;
				attackAtFrameIncrementInf = 400;
				break;
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
			badgerEntryPoint1 = actors["BadgerEntryPoint1"];
			badgerEntryPoint2 = actors["BadgerEntryPoint2"];
			sovietEntryPoint7 = actors["sovietEntryPoint7"];
			sovietinfantryentry1 = actors["SovietInfantryEntry1"];
			sovietinfantryrally1 = actors["SovietInfantryRally1"];
			paraDrop1 = actors["ParaDrop1"];
			paraDrop2 = actors["ParaDrop2"];
			barrack1 = actors["Barrack1"];
			sam1 = actors["Sam1"];
			sam2 = actors["Sam2"];

			var shroud = allies.PlayerActor.Trait<Shroud>();
			shroud.Explore(w, sam1.Location, 4);
			shroud.Explore(w, sam2.Location, 4);

			wr.Viewport.Center(alliesbase.CenterPosition);
			StartCountDownTimer();
			SendSquad1();
			SendSquad2();
			MissionUtils.PlayMissionMusic();
		}
	}
}
