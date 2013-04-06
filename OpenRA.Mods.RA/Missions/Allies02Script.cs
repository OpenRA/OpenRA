#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
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
		public event Action<bool> OnObjectivesUpdated = notify => { };

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
		Actor[] sams;
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
		const int SovietGroupSize = 5;

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

		const int ParatroopersTicks = 1500 * 5;
		static readonly string[] Badger1Passengers = { "e1", "e1", "e1", "e2", "3tnk" };
		static readonly string[] Badger2Passengers = { "e1", "e1", "e1", "e2", "e2" };
		static readonly string[] Badger3Passengers = { "e1", "e1", "e1", "e2", "e2" };

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
				if (sams.All(s => s.IsDead() || s.Owner != soviets))
				{
					objectives[DestroySamSitesID].Status = ObjectiveStatus.Completed;
					objectives[ExtractEinsteinID].Status = ObjectiveStatus.InProgress;
					OnObjectivesUpdated(true);
					world.CreateActor(SignalFlareName, new TypeDictionary { new OwnerInit(allies1), new LocationInit(extractionLZ.Location) });
					Sound.Play("flaren1.aud");
					ExtractEinsteinAtLZ();
				}
			}
			if (objectives[ExtractEinsteinID].Status == ObjectiveStatus.InProgress && einsteinChinook != null)
			{
				if (einsteinChinook.IsDead())
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

			if (tanya.IsDead())
				MissionFailed("Tanya was killed.");

			else if (einstein.IsDead())
				MissionFailed("Einstein was killed.");

			else if (!world.Actors.Any(a => (a.Owner == allies || a.Owner == allies2) && !a.IsDead()
				&& (a.HasTrait<Building>() && !a.HasTrait<Wall>()) || a.HasTrait<BaseBuilding>()))
			{
				objectives[MaintainPresenceID].Status = ObjectiveStatus.Failed;
				OnObjectivesUpdated(true);
				MissionFailed("The Allied reinforcements have been defeated.");
			}
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
			if (!sovietBarracks.IsDead())
				BuildSovietUnit(InfantryQueueName, SovietInfantry.Random(world.SharedRandom));

			if (!sovietWarFactory.IsDead())
			{
				var vehicles = world.FrameNumber >= SovietVehiclesUpgradeTicks ? SovietVehicles2 : SovietVehicles1;
				BuildSovietUnit(VehicleQueueName, vehicles.Random(world.SharedRandom));
			}
		}

		void ManageSovietUnits()
		{
			var units = world.FindAliveCombatantActorsInCircle(sovietRallyPoint.CenterLocation, 10)
				.Where(u => u.IsIdle && u.HasTrait<Mobile>() && u.HasTrait<AttackBase>() && u.Owner == soviets)
				.Except(world.WorldActor.Trait<SpawnMapActors>().Actors.Values);
			if (units.Count() >= SovietGroupSize)
			{
				foreach (var unit in units)
					MissionUtils.AttackNearestLandActor(true, unit, allies2);
			}

			var scatteredUnits = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && u.IsIdle
				&& u.HasTrait<Mobile>() && u.HasTrait<AttackBase>() && u.Owner == soviets)
				.Except(world.WorldActor.Trait<SpawnMapActors>().Actors.Values)
				.Except(units);

			foreach (var unit in scatteredUnits)
				MissionUtils.AttackNearestLandActor(true, unit, allies2);
		}

		void SetupAlliedBase()
		{
			foreach (var actor in world.Actors.Where(a => a.Owner == allies && a != allies.PlayerActor))
			{
				actor.ChangeOwner(allies2);
				if (actor.Info.Name == "pbox")
				{
					actor.AddTrait(new TransformedAction(s => s.Trait<Cargo>().Load(s, world.CreateActor(false, "e1", allies2, null, null))));
					actor.QueueActivity(new Transform(actor, "hbox.e1") { SkipMakeAnims = true });
				}
				if (actor.Info.Name == "proc")
					actor.QueueActivity(new Transform(actor, "proc") { SkipMakeAnims = true });
				foreach (var c in actor.TraitsImplementing<INotifyCapture>())
					c.OnCapture(actor, actor, allies, allies2);
			}
		}

		void InitializeSovietFactories()
		{
			var sbrp = sovietBarracks.Trait<RallyPoint>();
			var swrp = sovietWarFactory.Trait<RallyPoint>();
			sbrp.rallyPoint = swrp.rallyPoint = sovietRallyPoint.Location;
			sbrp.nearEnough = swrp.nearEnough = 6;
			sovietBarracks.Trait<PrimaryBuilding>().SetPrimaryProducer(sovietBarracks, true);
			sovietWarFactory.Trait<PrimaryBuilding>().SetPrimaryProducer(sovietWarFactory, true);
		}

		void BuildSovietUnit(string category, string unit)
		{
			var queue = MissionUtils.FindQueues(world, soviets, category).FirstOrDefault(q => q.CurrentItem() == null);
			if (queue == null) return;

			queue.ResolveOrder(queue.self, Order.StartProduction(queue.self, unit, 1));
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
				.Where(a => a.HasTrait<Mobile>()))
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

			soviets.PlayerActor.Trait<PlayerResources>().Cash = 1000;

			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			sam1 = actors["SAM1"];
			sam2 = actors["SAM2"];
			sam3 = actors["SAM3"];
			sam4 = actors["SAM4"];
			sams = new[] { sam1, sam2, sam3, sam4 };
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
			townPoint = actors["TownPoint"];
			sovietTownAttackPoint1 = actors["SovietTownAttackPoint1"];
			sovietTownAttackPoint2 = actors["SovietTownAttackPoint2"];
			yakEntryPoint = actors["YakEntryPoint"];
			yakAttackPoint = actors["YakAttackPoint"];

			SetupAlliedBase();

			var shroud = allies1.Shroud;
			shroud.Explore(w, sam1.Location, 2);
			shroud.Explore(w, sam2.Location, 2);
			shroud.Explore(w, sam3.Location, 2);
			shroud.Explore(w, sam4.Location, 2);

			if (w.ObserverMode || w.LocalPlayer == allies1)
				Game.MoveViewport(chinookHusk.Location.ToFloat2());

			else
				Game.MoveViewport(allies2BasePoint.Location.ToFloat2());

			MissionUtils.PlayMissionMusic();
		}
	}
}
