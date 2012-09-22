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
using OpenRA.Scripting;
using OpenRA.Traits;
using System;

namespace OpenRA.Mods.RA.Missions
{
	class Allies01ScriptInfo : TraitInfo<Allies01Script>, Requires<SpawnMapActorsInfo> { }

	class Allies01Script : IWorldLoaded, ITick
	{
		[Flags]
		enum Allies01Objectives
		{
			None = 0,
			FindEinstein = 1,
			WaitForHelicopter = 2
		}

		IEnumerable<string> GetObjectiveText()
		{
			var objectives = new List<string>();
			if (MissionUtils.HasFlag(currentObjectives, Allies01Objectives.FindEinstein))
			{
				objectives.Add("Find Einstein. Tanya and Einstein must survive.");
			}
			if (MissionUtils.HasFlag(currentObjectives, Allies01Objectives.WaitForHelicopter))
			{
				objectives.Add("Wait for the helicopter and extract Einstein. Tanya and Einstein must survive.");
			}
			return objectives;
		}

		Allies01Objectives currentObjectives = Allies01Objectives.FindEinstein;

		void DisplayObjective(string objective)
		{
			Game.AddChatLine(Color.LimeGreen, "Objective", objective);
			Sound.Play("bleep6.aud");
		}

		void DisplayHint(string objective)
		{
			Game.AddChatLine(Color.Yellow, "Hint", objective);
			Sound.Play("bleep6.aud");
		}

		void DisplayObjectives()
		{
			foreach (var objective in GetObjectiveText())
			{
				DisplayObjective(objective);
			}
		}

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
		const string ChinookName = "tran";
		const string SignalFlareName = "flare";

		void MissionFailed(string text)
		{
			if (allies.WinState != WinState.Undefined)
			{
				return;
			}
			allies.WinState = WinState.Lost;
			foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == allies && !a.IsDead()))
			{
				actor.Kill(actor);
			}
			Game.AddChatLine(Color.Red, "Mission failed", text);
			Sound.Play("misnlst1.aud");
		}

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

		public void Tick(Actor self)
		{
			if (allies.WinState != WinState.Undefined)
			{
				return;
			}
			if (world.FrameNumber % 1500 == 1)
			{
				DisplayObjectives();
			}
			if (world.FrameNumber % 1000 == 0)
			{
				Sound.Play(Taunts[world.SharedRandom.Next(Taunts.Length)]);
			}
			if (MissionUtils.HasFlag(currentObjectives, Allies01Objectives.FindEinstein))
			{
				if (AlliesControlLab())
				{
					SpawnSignalFlare();
					Sound.Play("flaren1.aud");
					SpawnEinsteinAtLab();
					SendShips();
					currentObjectives = MissionUtils.RemoveFlag(currentObjectives, Allies01Objectives.FindEinstein);
					currentObjectives = MissionUtils.AddFlag(currentObjectives, Allies01Objectives.WaitForHelicopter);
					DisplayObjectives();
					currentAttackWaveFrameNumber = world.FrameNumber;
				}
				if (lab.Destroyed)
				{
					MissionFailed("Einstein was killed.");
				}
			}
			else if (MissionUtils.HasFlag(currentObjectives, Allies01Objectives.WaitForHelicopter))
			{
				if (world.FrameNumber >= currentAttackWaveFrameNumber + 600)
				{
					Sound.Play("enmyapp1.aud");
					SendAttackWave(AttackWave);
					currentAttackWave++;
					currentAttackWaveFrameNumber = world.FrameNumber;
					if (currentAttackWave >= EinsteinChinookAttackWave)
					{
						SendAttackWave(LastAttackWaveAddition);
					}
					if (currentAttackWave == EinsteinChinookAttackWave)
					{
						ExtractEinsteinAtLZ();
					}
				}
				if (einsteinChinook != null)
				{
					if (einsteinChinook.Destroyed)
					{
						MissionFailed("The extraction helicopter was destroyed.");
					}
					else if (!world.Map.IsInMap(einsteinChinook.Location) && einsteinChinook.Trait<Cargo>().Passengers.Contains(einstein))
					{
						MissionAccomplished("Einstein was rescued.");
					}
				}
			}
			if (tanya != null && tanya.Destroyed)
			{
				MissionFailed("Tanya was killed.");
			}
			else if (einstein != null && einstein.Destroyed)
			{
				MissionFailed("Einstein was killed.");
			}
			ManageSovietOre();
		}

		void ManageSovietOre()
		{
			var res = soviets.PlayerActor.Trait<PlayerResources>();
			res.TakeOre(res.Ore);
			res.TakeCash(res.Cash);
		}

		void SpawnSignalFlare()
		{
			world.CreateActor(SignalFlareName, new TypeDictionary { new OwnerInit(allies), new LocationInit(extractionLZ.Location) });
		}

		void SendAttackWave(IEnumerable<string> wave)
		{
			foreach (var unit in wave)
			{
				var spawnActor = world.SharedRandom.Next(2) == 0 ? attackEntryPoint1 : attackEntryPoint2;
				var actor = world.CreateActor(unit, new TypeDictionary { new OwnerInit(soviets), new LocationInit(spawnActor.Location) });
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

		void SendPatrol()
		{
			for (int i = 0; i < Patrol.Length; i++)
			{
				var actor = world.CreateActor(Patrol[i], new TypeDictionary { new OwnerInit(soviets), new LocationInit(insertionLZ.Location + new CVec(-1 + i, 10 + i * 2)) });
				actor.QueueActivity(new Move.Move(insertionLZ.Location));
			}
		}

		bool AlliesControlLab()
		{
			return MissionUtils.AreaSecuredWithUnits(world, allies, lab.CenterLocation, LabClearRange);
		}

		void SpawnEinsteinAtLab()
		{
			einstein = world.CreateActor(EinsteinName, new TypeDictionary { new OwnerInit(allies), new LocationInit(lab.Location) });
			einstein.QueueActivity(new Move.Move(lab.Location - new CVec(0, 2)));
		}

		void SendShips()
		{
			for (int i = 0; i < Ships.Length; i++)
			{
				var actor = world.CreateActor(Ships[i],
					new TypeDictionary { new OwnerInit(allies), new LocationInit(shipSpawnPoint.Location + new CVec(i * 2, 0)) });
				actor.QueueActivity(new Move.Move(shipMovePoint.Location + new CVec(i * 4, 0)));
			}
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
			foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == allies && !a.IsDead()))
			{
				var at = actor.TraitOrDefault<AutoTarget>();
				if (at != null)
				{
					at.predictedStance = UnitStance.Defend;
				}
				var order = new Order("SetUnitStance", actor, false) { TargetLocation = new CPos((int)UnitStance.Defend, 0) };
				if (Game.IsHost)
				{
					world.IssueOrder(order);
				}
			}
		}

		public void WorldLoaded(World w)
		{
			world = w;
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
			SetAlliedUnitsToDefensiveStance();
			Game.MoveViewport(insertionLZ.Location.ToFloat2());
			Game.ConnectionStateChanged += StopMusic;
			Media.PlayFMVFullscreen(w, "ally1.vqa", () =>
			{
				Media.PlayFMVFullscreen(w, "landing.vqa", () =>
				{
					InsertTanyaAtLZ();
					SendPatrol();
					PlayMusic();
					DisplayObjectives();
				});
			});
		}

		void PlayMusic()
		{
			if (!Rules.InstalledMusic.Any())
			{
				return;
			}
			var track = Rules.InstalledMusic.Random(Game.CosmeticRandom);
			Sound.PlayMusicThen(track.Value, PlayMusic);
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
