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
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Missions
{
	class Allies02ScriptInfo : TraitInfo<Allies02Script>, Requires<SpawnMapActorsInfo> { }

	class Allies02Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event ObjectivesUpdatedEventHandler OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return objectives.Values; } }

		Dictionary<int, Objective> objectives = new Dictionary<int, Objective>()
		{
			{ FindEinsteinID, new Objective(ObjectiveType.Primary, FindEinstein, ObjectiveStatus.InProgress) },
			{ DestroySamSitesID, new Objective(ObjectiveType.Primary, DestroySamSites, ObjectiveStatus.InProgress) },
			{ ExtractEinsteinID, new Objective(ObjectiveType.Primary, ExtractEinstein, ObjectiveStatus.Inactive) },
			{ MaintainPresenceID, new Objective(ObjectiveType.Primary, MaintainPresence, ObjectiveStatus.InProgress) },
			{ FewDeathsID, new Objective(ObjectiveType.Secondary, "", ObjectiveStatus.InProgress) }
		};

		const int FindEinsteinID = 0;
		const int DestroySamSitesID = 1;
		const int ExtractEinsteinID = 2;
		const int MaintainPresenceID = 3;
		const int FewDeathsID = 4;

		const string FindEinstein = "Find Einstein's crashed helicopter. Tanya must survive.";
		const string DestroySamSites = "Destroy the SAM sites. Tanya must survive.";
		const string ExtractEinstein = "Wait for the helicopter and extract Einstein. Tanya and Einstein must survive.";
		const string MaintainPresence = "Maintain an Allied presence in the area. Reinforcements will arrive soon.";
		const string FewDeathsTemplate = "Lose fewer than {0}/{1} units.";

		const int DeathsThreshold = 200;

		Actor sam1;
		Actor sam2;
		Actor sam3;
		Actor sam4;
		Actor tanya;
		Actor einstein;
		Actor engineer;

		Actor chinookHusk;
		Actor allies2BasePoint;
		Actor reinforcementsEntryPoint;
		Actor extractionLZEntryPoint;
		Actor extractionLZ;
		Actor badgerEntryPoint1;
		Actor badgerEntryPoint2;
		Actor badgerDropPoint1;
		Actor badgerDropPoint2;
		Actor badgerDropPoint3;
		Actor parabombPoint1;
		Actor parabombPoint2;
		Actor sovietRallyPoint;
		Actor flamersEntryPoint;
		Actor tanksEntryPoint;
		Actor townPoint;
		Actor sovietTownAttackPoint1;
		Actor sovietTownAttackPoint2;
		Actor yakEntryPoint;
		Actor yakAttackPoint;
		Actor yak;

		Actor einsteinChinook;

		World world;
		Player allies;
		Player allies1;
		Player allies2;
		Player soviets;

		Actor sovietBarracks;
		Actor sovietWarFactory;

		CountdownTimer reinforcementsTimer;
		CountdownTimerWidget reinforcementsTimerWidget;

		const string InfantryQueueName = "Infantry";
		const string VehicleQueueName = "Vehicle";
		static readonly string[] SovietInfantry = { "e1", "e2", "e3" };
		static readonly string[] SovietVehicles1 = { "3tnk" };
		static readonly string[] SovietVehicles2 = { "3tnk", "v2rl" };
		const int SovietVehiclesUpgradeTicks = 1500 * 4;
		const int SovietGroupSize = 20;

		const int ReinforcementsTicks = 1500 * 12;
		static readonly string[] Reinforcements =
		{
			"2tnk", "2tnk", "2tnk", "2tnk", "2tnk", "2tnk",
			"1tnk", "1tnk",
			"jeep",
			"e1", "e1", "e1", "e1",
			"e3", "e3",
			"mcv",
			"truk", "truk", "truk", "truk", "truk", "truk"
		};
		int currentReinforcement = -1;

		const int ParabombTicks = 750;

		const int FlamersTicks = 1500 * 2;
		static readonly string[] Flamers = { "e4", "e4", "e4", "e4", "e4" };
		const string ApcName = "apc";

		const int ParatroopersTicks = 1500 * 5;
		static readonly string[] Badger1Passengers = { "e1", "e1", "e1", "e2", "3tnk" };
		static readonly string[] Badger2Passengers = { "e1", "e1", "e1", "e2", "e2" };
		static readonly string[] Badger3Passengers = { "e1", "e1", "e1", "e2", "e2" };

		const int TanksTicks = 1500 * 11;
		static readonly string[] Tanks = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk" };

		const string SignalFlareName = "flare";
		const string YakName = "yak";

		const int AlliedTownTransferRange = 15;
		const int SovietTownAttackGroupRange = 5;
		const int SovietTownMoveNearEnough = 3;

		void MissionFailed(string text)
		{
			MissionUtils.CoopMissionFailed(world, text, allies1, allies2);
			if (reinforcementsTimer != null)
				reinforcementsTimerWidget.Visible = false;
		}

		void MissionAccomplished(string text)
		{
			MissionUtils.CoopMissionAccomplished(world, text, allies1, allies2);
		}

		public void Tick(Actor self)
		{
			if (allies1.WinState != WinState.Undefined) return;

			if (world.FrameNumber % 50 == 1 && chinookHusk.IsInWorld)
				world.Add(new Smoke(world, chinookHusk.CenterLocation, "smoke_m"));

			if (world.FrameNumber == 1)
			{
				InitializeSovietFactories();
				StartReinforcementsTimer();
			}

			reinforcementsTimer.Tick();

			if (world.FrameNumber == ParatroopersTicks)
			{
				MissionUtils.Paradrop(world, soviets, Badger1Passengers, badgerEntryPoint1.Location, badgerDropPoint1.Location);
				MissionUtils.Paradrop(world, soviets, Badger2Passengers, badgerEntryPoint1.Location + new CVec(3, 0), badgerDropPoint2.Location);
				MissionUtils.Paradrop(world, soviets, Badger3Passengers, badgerEntryPoint1.Location + new CVec(6, 0), badgerDropPoint3.Location);
			}
			if (world.FrameNumber == ParabombTicks)
			{
				MissionUtils.Parabomb(world, soviets, badgerEntryPoint2.Location, parabombPoint1.Location);
				MissionUtils.Parabomb(world, soviets, badgerEntryPoint2.Location + new CVec(0, 3), parabombPoint2.Location);
			}

			if (allies1 != allies2)
			{
				if (world.FrameNumber == TanksTicks)
					RushSovietUnits();

				if (world.FrameNumber == FlamersTicks)
					RushSovietFlamers();

				if (yak == null || (yak != null && !yak.IsDead() && (yak.GetCurrentActivity() is FlyCircle || yak.IsIdle)))
				{
					var alliedUnitsNearYakPoint = world.FindAliveCombatantActorsInCircle(yakAttackPoint.CenterLocation, 10)
						.Where(a => a.Owner != soviets && a.HasTrait<IMove>() && a != tanya && a != einstein && a != engineer);
					if (alliedUnitsNearYakPoint.Any())
						YakStrafe(alliedUnitsNearYakPoint);
				}
			}

			if (currentReinforcement > -1 && currentReinforcement < Reinforcements.Length && world.FrameNumber % 25 == 0)
				SpawnAlliedUnit(Reinforcements[currentReinforcement++]);

			if (world.FrameNumber % 25 == 0)
			{
				BuildSovietUnits();
				ManageSovietUnits();
			}

			UpdateDeaths();

			if (objectives[FindEinsteinID].Status == ObjectiveStatus.InProgress)
			{
				if (AlliesNearTown())
				{
					objectives[FindEinsteinID].Status = ObjectiveStatus.Completed;
					OnObjectivesUpdated(true);
					TransferTownUnitsToAllies();
					SovietsAttackTown();
				}
			}
			if (objectives[DestroySamSitesID].Status == ObjectiveStatus.InProgress)
			{
				if ((sam1.Destroyed || sam1.Owner != soviets)
					&& (sam2.Destroyed || sam2.Owner != soviets)
					&& (sam3.Destroyed || sam3.Owner != soviets)
					&& (sam4.Destroyed || sam4.Owner != soviets))
				{
					objectives[DestroySamSitesID].Status = ObjectiveStatus.Completed;
					objectives[ExtractEinsteinID].Status = ObjectiveStatus.InProgress;
					OnObjectivesUpdated(true);
					SpawnSignalFlare();
					Sound.Play("flaren1.aud");
					ExtractEinsteinAtLZ();
				}
			}
			if (objectives[ExtractEinsteinID].Status == ObjectiveStatus.InProgress && einsteinChinook != null)
			{
				if (einsteinChinook.Destroyed)
				{
					objectives[ExtractEinsteinID].Status = ObjectiveStatus.Failed;
					objectives[MaintainPresenceID].Status = ObjectiveStatus.Failed;
					OnObjectivesUpdated(true);
					MissionFailed("The extraction helicopter was destroyed.");
				}
				else if (!world.Map.IsInMap(einsteinChinook.Location) && einsteinChinook.Trait<Cargo>().Passengers.Contains(einstein))
				{
					objectives[ExtractEinsteinID].Status = ObjectiveStatus.Completed;
					objectives[MaintainPresenceID].Status = ObjectiveStatus.Completed;

					if (objectives[FewDeathsID].Status == ObjectiveStatus.InProgress)
						objectives[FewDeathsID].Status = ObjectiveStatus.Completed;

					OnObjectivesUpdated(true);
					MissionAccomplished("Einstein was rescued.");
				}
			}

			if (tanya.Destroyed)
				MissionFailed("Tanya was killed.");

			else if (einstein.Destroyed)
				MissionFailed("Einstein was killed.");

			world.AddFrameEndTask(w =>
			{
				if (!world.FindAliveCombatantActorsInCircle(allies2BasePoint.CenterLocation, 20)
					.Any(a => a.HasTrait<Building>() && !a.HasTrait<Wall>() && (a.Owner == allies || a.Owner == allies2)))
				{
					objectives[MaintainPresenceID].Status = ObjectiveStatus.Failed;
					OnObjectivesUpdated(true);
					MissionFailed("The Allied reinforcements have been defeated.");
				}
			});
		}

		void UpdateDeaths()
		{
			var unitDeaths = allies1.Deaths + allies2.Deaths;
			objectives[FewDeathsID].Text = FewDeathsTemplate.F(unitDeaths, DeathsThreshold);
			OnObjectivesUpdated(false);
			if (unitDeaths >= DeathsThreshold && objectives[FewDeathsID].Status == ObjectiveStatus.InProgress)
			{
				objectives[FewDeathsID].Status = ObjectiveStatus.Failed;
				OnObjectivesUpdated(true);
			}
		}

		void YakStrafe(IEnumerable<Actor> candidates)
		{
			if (yak == null)
			{
				yak = world.CreateActor(YakName, new TypeDictionary
				{
					new LocationInit(yakEntryPoint.Location),
					new OwnerInit(soviets),
					new FacingInit(Util.GetFacing(yakAttackPoint.Location - yakEntryPoint.Location, 0)),
					new AltitudeInit(Rules.Info[YakName].Traits.Get<PlaneInfo>().CruiseAltitude)
				});
			}

			if (yak.Trait<LimitedAmmo>().HasAmmo())
				yak.QueueActivity(new FlyAttack(Target.FromActor(candidates.Random(world.SharedRandom))));

			else
			{
				yak.QueueActivity(new FlyOffMap());
				yak.QueueActivity(new RemoveSelf());
			}
		}

		void BuildSovietUnits()
		{
			if (!sovietBarracks.Destroyed)
				BuildSovietUnit(InfantryQueueName, SovietInfantry.Random(world.SharedRandom));

			if (!sovietWarFactory.Destroyed)
			{
				var vehicles = world.FrameNumber >= SovietVehiclesUpgradeTicks ? SovietVehicles2 : SovietVehicles1;
				BuildSovietUnit(VehicleQueueName, vehicles.Random(world.SharedRandom));
			}
		}

		void ManageSovietUnits()
		{
			var idleSovietUnitsAtRP = world.FindAliveCombatantActorsInCircle(sovietRallyPoint.CenterLocation, 3)
				.Where(a => a.Owner == soviets && a.IsIdle && a.HasTrait<IMove>());

			if (idleSovietUnitsAtRP.Count() >= SovietGroupSize)
			{
				var firstUnit = idleSovietUnitsAtRP.FirstOrDefault();
				if (firstUnit != null)
				{
					var closestAlliedBuilding = ClosestAlliedBuilding(firstUnit, 40);
					if (closestAlliedBuilding != null)
						foreach (var unit in idleSovietUnitsAtRP)
						{
							unit.Trait<Mobile>().Nudge(unit, unit, true);
							unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, new Attack(Target.FromActor(closestAlliedBuilding), 3)));
						}
				}
			}

			var idleSovietUnits = world.FindAliveCombatantActorsInCircle(allies2BasePoint.CenterLocation, 20)
				.Where(a => a.Owner == soviets && a.IsIdle && a.HasTrait<IMove>());

			foreach (var unit in idleSovietUnits)
			{
				var closestAlliedBuilding = ClosestAlliedBuilding(unit, 40);

				if (closestAlliedBuilding != null)
					unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, new Attack(Target.FromActor(closestAlliedBuilding), 3)));
			}
		}

		Actor ClosestAlliedBuilding(Actor actor, int range)
		{
			return MissionUtils.ClosestPlayerBuilding(world, allies2, actor.CenterLocation, range);
		}

		IEnumerable<Actor> ClosestAlliedBuildings(Actor actor, int range)
		{
			return MissionUtils.ClosestPlayerBuildings(world, allies2, actor.CenterLocation, range);
		}

		void InitializeSovietFactories()
		{
			var sbrp = sovietBarracks.Trait<RallyPoint>();
			var swrp = sovietWarFactory.Trait<RallyPoint>();
			sbrp.rallyPoint = swrp.rallyPoint = sovietRallyPoint.Location;
			sbrp.nearEnough = swrp.nearEnough = 3;
			sovietBarracks.Trait<PrimaryBuilding>().SetPrimaryProducer(sovietBarracks, true);
			sovietWarFactory.Trait<PrimaryBuilding>().SetPrimaryProducer(sovietWarFactory, true);
		}

		void BuildSovietUnit(string category, string unit)
		{
			var queue = MissionUtils.FindQueues(world, soviets, category).FirstOrDefault(q => q.CurrentItem() == null);
			if (queue == null) return;

			queue.ResolveOrder(queue.self, Order.StartProduction(queue.self, unit, 1));
		}

		void SpawnSignalFlare()
		{
			world.CreateActor(SignalFlareName, new TypeDictionary { new OwnerInit(allies1), new LocationInit(extractionLZ.Location) });
		}

		void StartReinforcementsTimer()
		{
			Sound.Play("timergo1.aud");
			reinforcementsTimer = new CountdownTimer(ReinforcementsTicks, ReinforcementsTimerExpired, true);
			reinforcementsTimerWidget = new CountdownTimerWidget(reinforcementsTimer, "Allied reinforcements arrive in: {0}");
			Ui.Root.AddChild(reinforcementsTimerWidget);
		}

		void ReinforcementsTimerExpired(CountdownTimer countdownTimer)
		{
			reinforcementsTimerWidget.Visible = false;
			currentReinforcement++;
			Sound.Play("aarrivs1.aud");
		}

		void SpawnAlliedUnit(string unit)
		{
			world.CreateActor(unit, new TypeDictionary
			{
				new LocationInit(reinforcementsEntryPoint.Location),
				new FacingInit(0),
				new OwnerInit(allies2)
			})
			.QueueActivity(new Move.Move(allies2BasePoint.Location));
		}

		void RushSovietUnits()
		{
			var closestAlliedBuildings = ClosestAlliedBuildings(badgerDropPoint1, 40);
			if (!closestAlliedBuildings.Any()) return;

			foreach (var tank in Tanks)
			{
				var unit = world.CreateActor(tank, new TypeDictionary 
				{
					new OwnerInit(soviets),
					new LocationInit(tanksEntryPoint.Location)
				});
				foreach (var building in closestAlliedBuildings)
					unit.QueueActivity(new Attack(Target.FromActor(building), 3));
			}
		}

		void RushSovietFlamers()
		{
			var closestAlliedBuilding = ClosestAlliedBuilding(badgerDropPoint1, 40);
			if (closestAlliedBuilding == null) return;

			var apc = world.CreateActor(ApcName, new TypeDictionary { new OwnerInit(soviets), new LocationInit(flamersEntryPoint.Location) });
			foreach (var flamer in Flamers)
			{
				var unit = world.CreateActor(false, flamer, new TypeDictionary { new OwnerInit(soviets) });
				apc.Trait<Cargo>().Load(apc, unit);
			}
			apc.QueueActivity(new MoveAdjacentTo(Target.FromActor(closestAlliedBuilding)));
			apc.QueueActivity(new UnloadCargo(true));
		}

		void ExtractEinsteinAtLZ()
		{
			einsteinChinook = MissionUtils.ExtractUnitWithChinook(
				world,
				allies1,
				einstein,
				extractionLZEntryPoint.Location,
				extractionLZ.Location,
				extractionLZEntryPoint.Location);
		}

		bool AlliesNearTown()
		{
			return world.FindAliveCombatantActorsInCircle(townPoint.CenterLocation, AlliedTownTransferRange)
				.Any(a => a.Owner == allies1 && a.HasTrait<IMove>());
		}

		void TransferTownUnitsToAllies()
		{
			foreach (var unit in world.FindAliveNonCombatantActorsInCircle(townPoint.CenterLocation, AlliedTownTransferRange)
				.Where(a => a.HasTrait<IMove>()))
				unit.ChangeOwner(allies1);
		}

		void SovietsAttackTown()
		{
			var sovietAttackUnits = world.FindAliveCombatantActorsInCircle(sovietTownAttackPoint1.CenterLocation, SovietTownAttackGroupRange)
				.Union(world.FindAliveCombatantActorsInCircle(sovietTownAttackPoint2.CenterLocation, SovietTownAttackGroupRange))
				.Union(world.FindAliveCombatantActorsInCircle(townPoint.CenterLocation, AlliedTownTransferRange))
				.Where(a => a.HasTrait<IMove>() && a.Owner == soviets);

			foreach (var unit in sovietAttackUnits)
				unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, new Move.Move(townPoint.Location, SovietTownMoveNearEnough)));
		}

		public void WorldLoaded(World w)
		{
			world = w;

			allies1 = w.Players.Single(p => p.InternalName == "Allies1");
			allies2 = w.Players.SingleOrDefault(p => p.InternalName == "Allies2");

			allies1.PlayerActor.Trait<PlayerResources>().Cash = 5000;
			if (allies2 == null)
				allies2 = allies1;
			else
				allies2.PlayerActor.Trait<PlayerResources>().Cash = 5000;

			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");

			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			sam1 = actors["SAM1"];
			sam2 = actors["SAM2"];
			sam3 = actors["SAM3"];
			sam4 = actors["SAM4"];
			tanya = actors["Tanya"];
			einstein = actors["Einstein"];
			engineer = actors["Engineer"];
			chinookHusk = actors["ChinookHusk"];
			allies2BasePoint = actors["Allies2BasePoint"];
			reinforcementsEntryPoint = actors["ReinforcementsEntryPoint"];
			extractionLZ = actors["ExtractionLZ"];
			extractionLZEntryPoint = actors["ExtractionLZEntryPoint"];
			badgerEntryPoint1 = actors["BadgerEntryPoint1"];
			badgerEntryPoint2 = actors["BadgerEntryPoint2"];
			badgerDropPoint1 = actors["BadgerDropPoint1"];
			badgerDropPoint2 = actors["BadgerDropPoint2"];
			badgerDropPoint3 = actors["BadgerDropPoint3"];
			parabombPoint1 = actors["ParabombPoint1"];
			parabombPoint2 = actors["ParabombPoint2"];
			sovietBarracks = actors["SovietBarracks"];
			sovietWarFactory = actors["SovietWarFactory"];
			sovietRallyPoint = actors["SovietRallyPoint"];
			flamersEntryPoint = actors["FlamersEntryPoint"];
			tanksEntryPoint = actors["TanksEntryPoint"];
			townPoint = actors["TownPoint"];
			sovietTownAttackPoint1 = actors["SovietTownAttackPoint1"];
			sovietTownAttackPoint2 = actors["SovietTownAttackPoint2"];
			yakEntryPoint = actors["YakEntryPoint"];
			yakAttackPoint = actors["YakAttackPoint"];

			SetupAlliedBase(actors);

			var shroud = w.WorldActor.Trait<Shroud>();
			shroud.Explore(w, sam1.Location, 2);
			shroud.Explore(w, sam2.Location, 2);
			shroud.Explore(w, sam3.Location, 2);
			shroud.Explore(w, sam4.Location, 2);

			if (w.LocalPlayer == null || w.LocalPlayer == allies1)
				Game.MoveViewport(chinookHusk.Location.ToFloat2());

			else
				Game.MoveViewport(allies2BasePoint.Location.ToFloat2());

			MissionUtils.PlayMissionMusic();
		}

		void SetupAlliedBase(Dictionary<string, Actor> actors)
		{
			world.AddFrameEndTask(w =>
			{
				foreach (var actor in actors.Where(a => a.Value.Owner == allies))
					actor.Value.ChangeOwner(allies2);

				world.CreateActor("proc", new TypeDictionary
				{
					new LocationInit(actors["Allies2ProcPoint"].Location),
					new OwnerInit(allies2)
				});
			});
		}
	}
}
