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
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	class MonsterTankMadnessScriptInfo : ITraitInfo, Requires<SpawnMapActorsInfo>
	{
		public readonly string[] FirstStartUnits = null;
		public readonly string[] SecondStartUnits = null;
		public readonly string[] ThirdStartUnits = null;
		public readonly string[] FirstBaseUnits = null;
		public readonly string[] CivilianEvacuees = null;

		public object Create(ActorInitializer init) { return new MonsterTankMadnessScript(this); }
	}

	class MonsterTankMadnessScript : IHasObjectives, IWorldLoaded, ITick
	{
		MonsterTankMadnessScriptInfo info;

		public MonsterTankMadnessScript(MonsterTankMadnessScriptInfo info)
		{
			this.info = info;
		}

		public event Action<bool> OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return new[] { findOutpost, evacuateDemitri, infiltrateRadarDome }; } }

		Objective findOutpost = new Objective(ObjectiveType.Primary, FindOutpostText, ObjectiveStatus.InProgress);
		Objective evacuateDemitri = new Objective(ObjectiveType.Primary, EvacuateDemitriText, ObjectiveStatus.InProgress);
		Objective infiltrateRadarDome = new Objective(ObjectiveType.Primary, InfiltrateRadarDomeText, ObjectiveStatus.InProgress);

		const string FindOutpostText = "Find our outpost and start repairs on it.";
		const string EvacuateDemitriText = "Find and evacuate Dr. Demitri. He is missing -- likely hiding in the village to the far south.";
		const string InfiltrateRadarDomeText = "Reprogram the Super Tanks by sending a spy into the Soviet radar dome.";

		//const string Briefing = "Dr. Demitri, creator of a Soviet Super Tank, wants to defect."
		//					+ " We planned to extract him while the Soviets were testing their new weapon, but something has gone wrong."
		//					+ " The Super Tanks are out of control, and Demitri is missing -- likely hiding in the village to the far south."
		//					+ " Find our outpost and start repairs on it, then find and evacuate Demitri."
		//					+ " As for the tanks, we can reprogram them. Send a spy into the Soviet radar dome in the NE, turning the tanks on their creators.";

		World world;

		Player neutral;
		Player greece;
		Player ussr;
		Player badGuy;
		Player turkey;

		Actor startEntryPoint;
		Actor startMovePoint;
		Actor startBridgeEndPoint;
		Actor alliedBaseTopLeft;
		Actor alliedBaseBottomRight;
		Actor alliedBaseEntryPoint;
		Actor alliedBaseMovePoint;

		Actor demitriChurch;
		Actor demitriChurchSpawnPoint;
		Actor demitriTriggerAreaCenter;
		Actor demitri;
		Actor demitriLZ;
		Actor demitriLZFlare;
		Actor demitriChinook;

		Actor provingGroundsCameraPoint;

		Actor[] superTanks;

		Actor hospital;
		Actor hospitalCivilianSpawnPoint;
		Actor hospitalSuperTankPoint;

		Actor superTankDome;

		bool hospitalEvacuated;
		bool superTanksDestroyed;

		int baseTransferredTick = -1;
		int superTankDomeInfiltratedTick = -1;

		void MissionAccomplished(string text)
		{
			MissionUtils.CoopMissionAccomplished(world, text, greece);
		}

		void MissionFailed(string text)
		{
			MissionUtils.CoopMissionFailed(world, text, greece);
		}

		public void Tick(Actor self)
		{
			if (greece.WinState != WinState.Undefined) return;

			if (world.FrameNumber == 1)
				SpawnAndMoveBridgeUnits(info.FirstStartUnits);

			else if (world.FrameNumber == 25 * 3)
				SpawnAndMoveBridgeUnits(info.SecondStartUnits);

			else if (world.FrameNumber == 25 * 8)
				SpawnAndMoveBridgeUnits(info.ThirdStartUnits);

			MissionUtils.CapOre(ussr);

			if (!hospitalEvacuated && !hospital.IsDead() && MissionUtils.AreaSecuredWithUnits(world, greece, hospital.CenterPosition, WDist.FromCells(5)))
			{
				EvacuateCivilians();
				hospitalEvacuated = true;
			}

			if (baseTransferredTick == -1)
			{
				var actorsInBase = world.FindActorsInBox(alliedBaseTopLeft.Location, alliedBaseBottomRight.Location).Where(a => a != a.Owner.PlayerActor);
				if (actorsInBase.Any(a => a.Owner == greece))
				{
					SetupAlliedBase(actorsInBase);
					baseTransferredTick = world.FrameNumber;
					findOutpost.Status = ObjectiveStatus.Completed;
					OnObjectivesUpdated(true);
				}
			}
			else if (superTankDomeInfiltratedTick == -1)
			{
				if (world.FrameNumber == baseTransferredTick + 25 * 100)
					foreach (var tank in superTanks.Where(t => !t.IsDead() && t.IsInWorld))
						tank.QueueActivity(false, new Move.Move(hospitalSuperTankPoint.Location, 2));

				else if (world.FrameNumber == baseTransferredTick + 25 * 180)
					foreach (var tank in superTanks.Where(t => !t.IsDead() && t.IsInWorld))
						tank.QueueActivity(false, new Move.Move(alliedBaseBottomRight.Location, 2));

				else if (world.FrameNumber == baseTransferredTick + 25 * 280)
					foreach (var tank in superTanks.Where(t => !t.IsDead() && t.IsInWorld))
						tank.QueueActivity(false, new Move.Move(demitriTriggerAreaCenter.Location, 2));

				else if (world.FrameNumber == baseTransferredTick + 25 * 480)
					foreach (var tank in superTanks.Where(t => !t.IsDead() && t.IsInWorld))
						tank.QueueActivity(false, new Move.Move(demitriLZ.Location, 4));
			}
			else
			{
				if (world.FrameNumber % 25 == 0)
					foreach (var tank in superTanks.Where(t => !t.IsDead() && t.IsInWorld && t.IsIdle))
						MissionUtils.AttackNearestLandActor(false, tank, ussr);
				if (world.FrameNumber == superTankDomeInfiltratedTick + 25 * 180)
				{
					foreach (var actor in world.Actors.Where(a => !a.IsDead() && (a.Owner == ussr || a.Owner == badGuy)))
						actor.Kill(actor);
				}
				if (world.FrameNumber == superTankDomeInfiltratedTick + 25 * 181)
				{
					foreach (var tank in superTanks.Where(t => !t.IsDead()))
						tank.Kill(tank);
					superTanksDestroyed = true;
				}
			}
			if (evacuateDemitri.Status != ObjectiveStatus.Completed)
			{
				if (demitri == null)
				{
					if (demitriChurch.IsDead())
					{
						evacuateDemitri.Status = ObjectiveStatus.Failed;
						OnObjectivesUpdated(true);
						MissionFailed("Dr. Demitri was killed.");
					}

					else if (MissionUtils.AreaSecuredWithUnits(world, greece, demitriTriggerAreaCenter.CenterPosition, WDist.FromCells(3)))
					{
						demitri = world.CreateActor("demitri", greece, demitriChurchSpawnPoint.Location, null);
						demitri.QueueActivity(new Move.Move(demitriTriggerAreaCenter.Location, 0));
						demitriLZFlare = world.CreateActor("flare", greece, demitriLZ.Location, null);
						Sound.Play("flaren1.aud");
						var chinookEntry = new CPos(demitriLZ.Location.X, 0);
						demitriChinook = MissionUtils.ExtractUnitWithChinook(world, greece, demitri, chinookEntry, demitriLZ.Location, chinookEntry);
					}
				}
				else if (demitri.IsDead())
				{
					evacuateDemitri.Status = ObjectiveStatus.Failed;
					OnObjectivesUpdated(true);
					MissionFailed("Dr. Demitri was killed.");
				}
				else if (demitriChinook != null && !demitriChinook.IsDead() && !world.Map.IsInMap(demitriChinook.Location) && demitriChinook.Trait<Cargo>().Passengers.Contains(demitri))
				{
					demitriLZFlare.Destroy();
					SpawnAndMoveAlliedBaseUnits(info.FirstBaseUnits);
					evacuateDemitri.Status = ObjectiveStatus.Completed;
					OnObjectivesUpdated(true);
				}
			}
			if (!world.Actors.Any(a => a.Owner == greece && a.IsInWorld && !a.IsDead()
				&& ((a.HasTrait<Building>() && !a.HasTrait<Wall>()) || a.HasTrait<BaseBuilding>() || a.HasTrait<Mobile>())))
			{
				MissionFailed("The remaining Allied forces in the area have been wiped out.");
			}
			if (superTankDomeInfiltratedTick == -1 && superTankDome.IsDead())
			{
				infiltrateRadarDome.Status = ObjectiveStatus.Failed;
				OnObjectivesUpdated(true);
				MissionFailed("The Soviet radar dome was destroyed.");
			}
			if (superTanksDestroyed && evacuateDemitri.Status == ObjectiveStatus.Completed)
			{
				MissionAccomplished("Dr. Demitri has been extracted and the super tanks have been dealt with.");
			}
		}

		void SetupAlliedBase(IEnumerable<Actor> actors)
		{
			foreach (var actor in actors)
			{
				// hack hack hack
				actor.ChangeOwner(greece);
				if (actor.Info.Name == "pbox")
				{
					actor.AddTrait(new TransformedAction(s => s.Trait<Cargo>().Load(s, world.CreateActor(false, "e1", greece, null, null))));
					actor.QueueActivity(new Transform(actor, "hbox.e1") { SkipMakeAnims = true });
				}
				else if (actor.Info.Name == "proc")
					actor.QueueActivity(new Transform(actor, "proc") { SkipMakeAnims = true });
				foreach (var c in actor.TraitsImplementing<INotifyCapture>())
					c.OnCapture(actor, actor, neutral, greece);
			}
		}

		void EvacuateCivilians()
		{
			foreach (var unit in info.CivilianEvacuees)
			{
				var actor = world.CreateActor(unit, neutral, hospitalCivilianSpawnPoint.Location, null);
				actor.Trait<Mobile>().Nudge(actor, actor, true);
				actor.QueueActivity(new Move.Move(alliedBaseEntryPoint.Location, 0));
				actor.QueueActivity(new RemoveSelf());
			}
		}

		void SpawnAndMoveBridgeUnits(string[] units)
		{
			Sound.Play("reinfor1.aud");
			foreach (var unit in units)
				world.CreateActor(unit, greece, startEntryPoint.Location, Traits.Util.GetFacing(startBridgeEndPoint.CenterPosition - startEntryPoint.CenterPosition, 0))
				.QueueActivity(new Move.Move(startMovePoint.Location, 0));
		}

		void SpawnAndMoveAlliedBaseUnits(string[] units)
		{
			Sound.Play("reinfor1.aud");
			foreach (var unit in units)
				world.CreateActor(unit, greece, alliedBaseEntryPoint.Location, Traits.Util.GetFacing(alliedBaseMovePoint.CenterPosition - alliedBaseEntryPoint.CenterPosition, 0))
				.QueueActivity(new Move.Move(alliedBaseMovePoint.Location, 0));
		}

		void OnSuperTankDomeInfiltrated(Actor spy)
		{
			if (superTankDomeInfiltratedTick != -1) return;

			superTankDome.QueueActivity(new Transform(superTankDome, "dome") { SkipMakeAnims = true });

			world.AddFrameEndTask(_ =>
			{
				superTanks.Do(world.Remove);
				turkey.Stances[greece] = turkey.Stances[neutral] = Stance.Ally;
				greece.Stances[turkey] = neutral.Stances[turkey] = Stance.Ally;
				greece.Shroud.ExploreAll(world);
				superTanks.Do(world.Add);
			});

			foreach (var tank in superTanks.Where(t => !t.IsDead() && t.IsInWorld))
				MissionUtils.AttackNearestLandActor(false, tank, ussr);

			superTankDomeInfiltratedTick = world.FrameNumber;

			infiltrateRadarDome.Status = ObjectiveStatus.Completed;
			OnObjectivesUpdated(true);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;

			neutral = w.Players.Single(p => p.InternalName == "Neutral");
			greece = w.Players.Single(p => p.InternalName == "Greece");
			ussr = w.Players.Single(p => p.InternalName == "USSR");
			badGuy = w.Players.Single(p => p.InternalName == "BadGuy");
			turkey = w.Players.Single(p => p.InternalName == "Turkey");

			greece.PlayerActor.Trait<PlayerResources>().Cash = 0;
			ussr.PlayerActor.Trait<PlayerResources>().Cash = 2000;

			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			startEntryPoint = actors["StartEntryPoint"];
			startMovePoint = actors["StartMovePoint"];
			startBridgeEndPoint = actors["StartBridgeEndPoint"];
			alliedBaseTopLeft = actors["AlliedBaseTopLeft"];
			alliedBaseBottomRight = actors["AlliedBaseBottomRight"];
			alliedBaseEntryPoint = actors["AlliedBaseEntryPoint"];
			alliedBaseMovePoint = actors["AlliedBaseMovePoint"];

			demitriChurch = actors["DemitriChurch"];
			demitriChurchSpawnPoint = actors["DemitriChurchSpawnPoint"];
			demitriTriggerAreaCenter = actors["DemitriTriggerAreaCenter"];
			demitriLZ = actors["DemitriLZ"];

			hospital = actors["Hospital"];
			hospitalCivilianSpawnPoint = actors["HospitalCivilianSpawnPoint"];
			hospitalSuperTankPoint = actors["HospitalSuperTankPoint"];

			superTanks = actors.Values.Where(a => a.Info.Name == "5tnk" && a.Owner == turkey).ToArray();

			provingGroundsCameraPoint = actors["ProvingGroundsCameraPoint"];
			world.CreateActor("camera", greece, provingGroundsCameraPoint.Location, null);

			superTankDome = actors["SuperTankDome"];
			superTankDome.AddTrait(new InfiltrateAction(OnSuperTankDomeInfiltrated));
			superTankDome.AddTrait(new TransformedAction(self => superTankDome = self));

			wr.Viewport.Center(startEntryPoint.CenterPosition);
			MissionUtils.PlayMissionMusic();
		}
	}
}
