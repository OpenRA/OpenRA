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
using System.Drawing;
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
	class FortLonestarScriptInfo : TraitInfo<FortLonestarScript>, Requires<SpawnMapActorsInfo> { }

	class FortLonestarScript : IWorldLoaded, ITick
	{
		Player multi0;
		Player soviets;

		Actor entry1;
		Actor entry2;
		Actor entry3;
		Actor entry4;
		Actor entry5;
		Actor entry6;
		Actor entry7;
		Actor entry8;
		CPos[] sovietEntryPoints;
		Actor paradrop1;
		Actor paradrop2;
		Actor paradrop3;
		Actor paradrop4;
		Actor patrol1;
		Actor patrol2;
		Actor patrol3;
		Actor patrol4;
		World world;
		
		int WaveNumber = 0;
		InfoWidget evacuateWidget;
		const string ShortEvacuateTemplate = "Wave {0}";
		static readonly string[] Patrol = { "e1", "e2", "e1" };
		static readonly string[] Infantry = { "e4", "e1", "e1", "e2", "e1", "e2" };
		static readonly string[] Vehicles = { "arty", "ftrk", "ftrk", "apc", "apc" };
		const string tank = "3tnk";
		const string v2 = "v2rl";
		const string boss = "4tnk";

		const int TimerTicks = 1;

		int AttackSquad = 6;
		int AttackSquadCount = 1;
		int VehicleSquad = 2;
		int VehicleSquadCount = 1;

		int patrolAttackFrame;
		int patrolattackAtFrameIncrement;
		int WaveAttackFrame;
		int WaveAttackAtFrameIncrement;
		int VehicleAttackFrame;
		int VehicleAttackAtFrameIncrement;

		void MissionAccomplished(string text)
		{
			MissionUtils.CoopMissionAccomplished(world, text, multi0);
		}

		void AttackNearestAlliedActor(Actor self)
		{
			var enemies = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && (u.Owner == multi0)
				&& ((u.HasTrait<Building>() && !u.HasTrait<Mobile>())));

			var targetEnemy = enemies.ClosestTo(self);
			if (targetEnemy != null)
				self.QueueActivity(new AttackMove.AttackMoveActivity(self, new Attack(Target.FromActor(targetEnemy), WDist.FromCells(6))));
		}

		void SendVehicles()
		{
			if (SpawnVehicles == true)
			{
				for (int i = 1; i <= VehicleSquadCount; i++)
				{
					var route = world.SharedRandom.Next(sovietEntryPoints.Length);
					var spawnPoint = sovietEntryPoints[route];
					for (int r = 1; r <= VehicleSquad; r++)
					{
						var squad = world.CreateActor(Vehicles.Random(world.SharedRandom),
							new TypeDictionary 
							{ 
								new LocationInit(spawnPoint), 
								new OwnerInit(soviets) 
							});
						squad.QueueActivity(new AttackMove.AttackMoveActivity(squad, new Move.Move(paradrop1.Location, 3)));
					}
				}
			}
		}

		void SendWave()
		{
			if (SpawnWave == true)
			{
				for (int i = 1; i <= AttackSquadCount; i++)
				{
					world.Actors.Where(u => u.IsInWorld && !u.IsDead() && (u.Owner == soviets)
						&& !u.HasTrait<Mobile>());
					var route = world.SharedRandom.Next(sovietEntryPoints.Length);
					var spawnPoint = sovietEntryPoints[route];
					for (int r = 1; r < AttackSquad; r++)
					{
						var squad = world.CreateActor(Infantry.Random(world.SharedRandom),
							new TypeDictionary 
							{ 
								new LocationInit(spawnPoint), 
								new OwnerInit(soviets) 
							});
						squad.QueueActivity(new AttackMove.AttackMoveActivity(squad, new Move.Move(paradrop1.Location, 3)));
						var scatteredUnits = world.FindAliveCombatantActorsInCircle(paradrop1.Location.CenterPosition, WDist.FromCells(15))
						.Where(unit => unit.IsIdle && unit.HasTrait<Mobile>() && unit.Owner == soviets);
						foreach (var unit in scatteredUnits)
						{
							AttackNearestAlliedActor(unit);
						}
					}
				}
			}
		}

		void SendPatrol(string[] squad, Player owner, CPos location, CPos move)
		{
			if (SpawnPatrol)
			{
				for (int i = 0; i < squad.Length; i++)
				{
					var actor = world.CreateActor(squad[i], new TypeDictionary 
				{ 
					new OwnerInit(owner), 
					new LocationInit(location) 
				});
					actor.QueueActivity(new AttackMove.AttackMoveActivity(actor, new Move.Move(move, 3)));
					AttackNearestAlliedActor(actor);
				}
			}
		}

		void SendBoss(string unit)
		{
			var route = world.SharedRandom.Next(sovietEntryPoints.Length);
			var spawnPoint = sovietEntryPoints[route];
			var actor = world.CreateActor(unit, new TypeDictionary 
				{ 
					new OwnerInit(soviets), 
					new LocationInit(spawnPoint) 
				});
			if (multi0 != null)
			{
				AttackNearestAlliedActor(actor);
			}
		}

		void Wave(string text)
		{
			Game.AddChatLine(Color.Cyan, "Wave Sequence", text);
		}

		public void Tick(Actor self)
		{
			if (multi0.WinState != WinState.Undefined) return;

			if (world.FrameNumber == patrolAttackFrame)
			{
				patrolAttackFrame += patrolattackAtFrameIncrement;
				patrolattackAtFrameIncrement = Math.Max(patrolattackAtFrameIncrement - 5, 100);
				SendPatrol(Patrol, soviets, patrol1.Location, paradrop1.Location);
				SendPatrol(Patrol, soviets, patrol2.Location, paradrop2.Location);
				SendPatrol(Patrol, soviets, patrol3.Location, paradrop3.Location);
				SendPatrol(Patrol, soviets, patrol4.Location, paradrop4.Location);
			}
			if (world.FrameNumber == WaveAttackFrame)
			{
				WaveAttackFrame += WaveAttackAtFrameIncrement;
				WaveAttackAtFrameIncrement = Math.Max(WaveAttackAtFrameIncrement - 5, 100);
				SendWave();
			}
			if (world.FrameNumber == VehicleAttackFrame)
			{
				VehicleAttackFrame += VehicleAttackAtFrameIncrement;
				VehicleAttackAtFrameIncrement = Math.Max(VehicleAttackAtFrameIncrement - 5, 100);
				SendVehicles();
			}
			if (world.FrameNumber == TimerTicks)
			{
				evacuateWidget = new InfoWidget("");
				Ui.Root.AddChild(evacuateWidget);
				WaveNumber++;
				Wave("One Initializing");
				UpdateWaveSequence();
			}
			if (world.FrameNumber == 1500 * 2)
			{
				WaveNumber++;
				Wave("Two Initializing");
				SpawnPatrol = false;
				AttackSquad = 7;
				AttackSquadCount = 2;
				UpdateWaveSequence();
				MissionUtils.Parabomb(world, soviets, entry1.Location, paradrop1.Location);
				MissionUtils.Parabomb(world, soviets, entry1.Location, paradrop1.Location + new CVec(0, -2));
			}
			if (world.FrameNumber == 1500 * 4)
			{
				WaveNumber++;
				Wave("Three Initializing");
				UpdateWaveSequence();
				AttackSquad = 8;
			}
			if (world.FrameNumber == 1500 * 6)
			{
				WaveNumber++;
				Wave("Four Initializing");
				UpdateWaveSequence();
				AttackSquad = 9;
				MissionUtils.Parabomb(world, soviets, entry1.Location, paradrop1.Location);
				MissionUtils.Parabomb(world, soviets, entry2.Location, paradrop3.Location);
				AttackSquadCount = 3;
				VehicleSquad = 3;
			}
			if (world.FrameNumber == 1500 * 8)
			{
				WaveNumber++;
				Wave("Five Initializing");
				UpdateWaveSequence();
				AttackSquad = 10;
				VehicleSquad = 4;
				VehicleSquadCount = 2;
				SendBoss(tank);
			}
			if (world.FrameNumber == 1500 * 11)
			{
				WaveNumber++;
				Wave("Six Initializing");
				UpdateWaveSequence();
				AttackSquad = 11;
				AttackSquadCount = 4;
				MissionUtils.Parabomb(world, soviets, entry1.Location, paradrop1.Location);
				MissionUtils.Parabomb(world, soviets, entry4.Location, paradrop1.Location);
				MissionUtils.Parabomb(world, soviets, entry6.Location, paradrop3.Location);
				MissionUtils.Parabomb(world, soviets, entry5.Location, paradrop3.Location);
				SendBoss(tank);
				SendBoss(tank);
			}
			if (world.FrameNumber == 1500 * 14)
			{
				WaveNumber++;
				Wave("Seven Initializing");
				UpdateWaveSequence();
				AttackSquad = 12;
				VehicleSquad = 5;
				VehicleSquadCount = 3;
				SendBoss(v2);
			}
			if (world.FrameNumber == 1500 * 17)
			{
				SpawnVehicles = true;
				WaveNumber++;
				Wave("Eight Initializing");
				UpdateWaveSequence();
				AttackSquad = 13;
				AttackSquadCount = 5;
				MissionUtils.Parabomb(world, soviets, entry1.Location, paradrop1.Location);
				MissionUtils.Parabomb(world, soviets, entry4.Location, paradrop1.Location);
				MissionUtils.Parabomb(world, soviets, entry6.Location, paradrop3.Location);
				MissionUtils.Parabomb(world, soviets, entry5.Location, paradrop3.Location);
				MissionUtils.Parabomb(world, soviets, entry2.Location, paradrop2.Location);
				MissionUtils.Parabomb(world, soviets, entry3.Location, paradrop2.Location);
				SendBoss(v2);
				SendBoss(tank);
				SendBoss(v2);
			}
			if (world.FrameNumber == 1500 * 21)
			{
				WaveNumber++;
				Wave("Nine Initializing");
				UpdateWaveSequence();
				AttackSquad = 14;
				VehicleSquad = 6;
				VehicleSquadCount = 4;
				SendBoss(v2);
				SendBoss(tank);
				SendBoss(tank);
			}
			if (world.FrameNumber == 1500 * 25)
			{
				WaveNumber++;
				Wave("Ten Initializing");
				UpdateWaveSequence();
				AttackSquad = 15;
				AttackSquadCount = 6;
				for (int i = 0; i < 2; i++)
				{
					MissionUtils.Parabomb(world, soviets, entry1.Location, paradrop1.Location + new CVec(0, -2));
					MissionUtils.Parabomb(world, soviets, entry2.Location, paradrop3.Location + new CVec(0, -2));
					MissionUtils.Parabomb(world, soviets, entry4.Location, paradrop2.Location + new CVec(0, -2));
					MissionUtils.Parabomb(world, soviets, entry5.Location, paradrop4.Location + new CVec(0, -2));
					MissionUtils.Parabomb(world, soviets, entry2.Location, paradrop1.Location + new CVec(0, 2));
					MissionUtils.Parabomb(world, soviets, entry4.Location, paradrop3.Location + new CVec(0, 2));
					MissionUtils.Parabomb(world, soviets, entry3.Location, paradrop2.Location + new CVec(0, 2));
					MissionUtils.Parabomb(world, soviets, entry5.Location, paradrop4.Location + new CVec(0, 2));
				}
				SendBoss(boss);
			}
			if (world.FrameNumber == 1500 * 30)
			{
				SpawnWave = false;
				SpawnVehicles = false;
			} 
			if (world.FrameNumber == 1500 * 31)
			{
				MissionAccomplished("You and your mates have Survived the Onslaught!");
			}
		}

		void UpdateWaveSequence()
		{
			evacuateWidget.Text = ShortEvacuateTemplate.F(WaveNumber);
		}

		bool SpawnPatrol = true;

		bool SpawnWave = true;

		bool SpawnVehicles = true;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			multi0 = w.Players.Single(p => p.InternalName == "Multi0");
			patrolAttackFrame = 750;
			patrolattackAtFrameIncrement = 750;
			WaveAttackFrame = 500;
			WaveAttackAtFrameIncrement = 500;
			VehicleAttackFrame = 2000;
			VehicleAttackAtFrameIncrement = 2000;
			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			entry1 = actors["Entry1"];
			entry2 = actors["Entry2"];
			entry3 = actors["Entry3"];
			entry4 = actors["Entry4"];
			entry5 = actors["Entry5"];
			entry6 = actors["Entry6"];
			entry7 = actors["Entry7"];
			entry8 = actors["Entry8"];
			sovietEntryPoints = new[] { entry1, entry2, entry3, entry4, entry5, entry6, entry7, entry8 }.Select(p => p.Location).ToArray();
			paradrop1 = actors["Paradrop1"];
			paradrop2 = actors["Paradrop2"];
			paradrop3 = actors["Paradrop3"];
			paradrop4 = actors["Paradrop4"];
			patrol1 = actors["Patrol1"];
			patrol2 = actors["Patrol2"];
			patrol3 = actors["Patrol3"];
			patrol4 = actors["Patrol4"];
			MissionUtils.PlayMissionMusic();
			Game.AddChatLine(Color.Cyan, "Mission", "Defend Fort LoneStar At All costs!");
		}
	}
}
