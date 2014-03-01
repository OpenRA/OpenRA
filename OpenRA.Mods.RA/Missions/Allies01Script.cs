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
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Move;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	class Allies01ScriptInfo : TraitInfo<Allies01Script>, Requires<SpawnMapActorsInfo> { }

	class Allies01Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event Action<bool> OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return new[] { findEinstein, extractEinstein }; } }

		Objective findEinstein = new Objective(ObjectiveType.Primary, FindEinsteinText, ObjectiveStatus.InProgress);
		Objective extractEinstein = new Objective(ObjectiveType.Primary, ExtractEinsteinText, ObjectiveStatus.Inactive);

		const string FindEinsteinText = "Find Einstein. Tanya and Einstein must survive.";
		const string ExtractEinsteinText = "Wait for the helicopter and extract Einstein. Tanya and Einstein must survive.";

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

		World world;

		static readonly string[] Taunts = { "laugh1.aud", "lefty1.aud", "cmon1.aud", "gotit1.aud" };

		static readonly string[] Ships = { "ca", "ca", "ca", "ca" };
		static readonly string[] Patrol = { "e1", "dog", "e1" };

		static readonly string[] AttackWave = { "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e2", "dog" };
		static readonly string[] LastAttackWaveAddition = { "3tnk", "e1", "e1", "e1", "e1", "e2", "e2", "e2", "e2" };
		int currentAttackWaveFrameNumber;
		int currentAttackWave;
		const int EinsteinChinookAttackWave = 5;

		const int LabClearRange = 5;
		const string EinsteinName = "einstein";
		const string TanyaName = "e7";
		const string SignalFlareName = "flare";

		string difficulty;

		void MissionAccomplished(string text)
		{
			MissionUtils.CoopMissionAccomplished(world, text, allies);
		}

		void MissionFailed(string text)
		{
			MissionUtils.CoopMissionFailed(world, text, allies);
		}

		public void Tick(Actor self)
		{
			if (allies.WinState != WinState.Undefined) return;

			if (world.FrameNumber % 1000 == 0)
				Sound.Play(Taunts[world.SharedRandom.Next(Taunts.Length)]);

			if (findEinstein.Status == ObjectiveStatus.InProgress)
			{
				if (AlliesControlLab())
					LabSecured();

				if (lab.IsDead())
				{
					findEinstein.Status = ObjectiveStatus.Failed;
					OnObjectivesUpdated(true);
					MissionFailed("Einstein was killed.");
				}
			}
			if (extractEinstein.Status == ObjectiveStatus.InProgress)
			{
				if (difficulty != "Easy")
				{
					ManageSovietUnits();
					if (world.FrameNumber >= currentAttackWaveFrameNumber + 400)
					{
						SpawnSovietUnits(AttackWave);
						currentAttackWave++;
						currentAttackWaveFrameNumber = world.FrameNumber;

						if (currentAttackWave >= EinsteinChinookAttackWave)
							SpawnSovietUnits(LastAttackWaveAddition);

						if (currentAttackWave == EinsteinChinookAttackWave)
							ExtractEinsteinAtLZ();
					}
				}
				if (einsteinChinook != null)
				{
					if (einsteinChinook.IsDead())
					{
						extractEinstein.Status = ObjectiveStatus.Failed;
						OnObjectivesUpdated(true);
						MissionFailed("The extraction helicopter was destroyed.");
					}
					else if (!world.Map.IsInMap(einsteinChinook.Location) && einsteinChinook.Trait<Cargo>().Passengers.Contains(einstein))
					{
						extractEinstein.Status = ObjectiveStatus.Completed;
						OnObjectivesUpdated(true);
						MissionAccomplished("Einstein was rescued");
					}
				}
			}

			if (tanya != null && tanya.IsDead())
				MissionFailed("Tanya was killed.");

			else if (einstein != null && einstein.IsDead())
				MissionFailed("Einstein was killed.");

			MissionUtils.CapOre(soviets);
		}

		void LabSecured()
		{
			SpawnSignalFlare();
			Sound.Play("flaren1.aud");
			SpawnEinsteinAtLab();
			SendShips();
			lab.QueueActivity(new Transform(lab, "stek") { SkipMakeAnims = true });

			findEinstein.Status = ObjectiveStatus.Completed;
			extractEinstein.Status = ObjectiveStatus.InProgress;
			OnObjectivesUpdated(true);

			currentAttackWaveFrameNumber = world.FrameNumber;

			if (difficulty == "Easy")
				ExtractEinsteinAtLZ();
			else
			{
				var infantry = MissionUtils.FindQueues(world, soviets, "Infantry").FirstOrDefault();
				if (infantry != null)
					infantry.ResolveOrder(infantry.self, Order.StartProduction(infantry.self, "e1", 5));
			}
		}

		void SpawnSignalFlare()
		{
			world.CreateActor(SignalFlareName, new TypeDictionary { new OwnerInit(allies), new LocationInit(extractionLZ.Location) });
		}

		void SpawnSovietUnits(IEnumerable<string> wave)
		{
			foreach (var unit in wave)
			{
				var spawnActor = world.SharedRandom.Next(2) == 0 ? attackEntryPoint1 : attackEntryPoint2;
				world.CreateActor(unit, new TypeDictionary { new OwnerInit(soviets), new LocationInit(spawnActor.Location) });
			}
		}

		void ManageSovietUnits()
		{
			foreach (var unit in world.Actors.Where(u => u.IsInWorld && u.Owner == soviets && !u.IsDead() && u.IsIdle
				&& u.HasTrait<Mobile>() && u.HasTrait<AttackBase>()))
			{
				Activity innerActivity;
				if (einstein != null)
				{
					if (einstein.IsInWorld)
						innerActivity = new Move.Move(Target.FromActor(einstein), WDist.FromCells(3));

					else
					{
						var container = world.UnitContaining(einstein);

						if (container != null && !container.HasTrait<Aircraft>() && container.HasTrait<Mobile>())
							innerActivity = new Move.Move(Target.FromActor(container), WDist.FromCells(3));

						else
							innerActivity = new Move.Move(extractionLZ.Location, 3);
					}
					unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, innerActivity));
				}
			}
		}

		void SendPatrol()
		{
			for (int i = 0; i < Patrol.Length; i++)
				world.CreateActor(Patrol[i], new TypeDictionary
				{
					new OwnerInit(soviets),
					new LocationInit(insertionLZ.Location + new CVec(-1 + i, 10 + i * 2))
				})
				.QueueActivity(new Move.Move(insertionLZ.Location));
		}

		bool AlliesControlLab()
		{
			return MissionUtils.AreaSecuredWithUnits(world, allies, lab.CenterPosition, WDist.FromCells(LabClearRange));
		}

		void SpawnEinsteinAtLab()
		{
			einstein = world.CreateActor(EinsteinName, new TypeDictionary { new OwnerInit(allies), new LocationInit(lab.Location) });
			einstein.QueueActivity(new Move.Move(lab.Location - new CVec(0, 2)));
		}

		void SendShips()
		{
			for (int i = 0; i < Ships.Length; i++)
				world.CreateActor(Ships[i], new TypeDictionary
				{
					new OwnerInit(allies),
					new LocationInit(shipSpawnPoint.Location + new CVec(i * 2, 0))
				})
				.QueueActivity(new Move.Move(shipMovePoint.Location + new CVec(i * 4, 0)));
		}

		void ExtractEinsteinAtLZ()
		{
			einsteinChinook = MissionUtils.ExtractUnitWithChinook(
				world,
				allies,
				einstein,
				extractionLZEntryPoint.Location,
				extractionLZ.Location,
				chinookExitPoint.Location);
		}

		void InsertTanyaAtLZ()
		{
			tanya = MissionUtils.InsertUnitWithChinook(
				world,
				allies,
				TanyaName,
				insertionLZEntryPoint.Location,
				insertionLZ.Location,
				chinookExitPoint.Location,
				unit =>
				{
					Sound.Play("laugh1.aud");
					unit.QueueActivity(new Move.Move(insertionLZ.Location - new CVec(1, 0)));
				}).Second;
		}

		void SetAlliedUnitsToDefensiveStance()
		{
			foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == allies && !a.IsDead() && a.HasTrait<AutoTarget>()))
				actor.Trait<AutoTarget>().Stance = UnitStance.Defend;
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;

			difficulty = w.LobbyInfo.GlobalSettings.Difficulty;
			Game.Debug("{0} difficulty selected".F(difficulty));

			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");

			allies.PlayerActor.Trait<PlayerResources>().Cash = 0;

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
			SetAlliedUnitsToDefensiveStance();

			wr.Viewport.Center(insertionLZ.CenterPosition);

			if (w.LobbyInfo.IsSinglePlayer)
				Media.PlayFMVFullscreen(w, "ally1.vqa", () =>
					Media.PlayFMVFullscreen(w, "landing.vqa", () =>
					{
						InsertTanyaAtLZ();
						SendPatrol();
						MissionUtils.PlayMissionMusic();
					})
				);
			else
			{
				InsertTanyaAtLZ();
				SendPatrol();
				MissionUtils.PlayMissionMusic();
			}
		}
	}
}
