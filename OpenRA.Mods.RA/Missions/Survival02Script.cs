#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using System;
using System.Drawing;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Widgets;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Missions
{
	class Survival02ScriptInfo : TraitInfo<Survival02Script>, Requires<SpawnMapActorsInfo> { }

	class Survival02Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event Action<bool> OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return objectives.Values; } }

		Dictionary<int, Objective> objectives = new Dictionary<int, Objective>
		{
			{ maintainPresenceID, new Objective(ObjectiveType.Primary, maintainPresence, ObjectiveStatus.InProgress) },
			{ destroySovietsID, new Objective(ObjectiveType.Primary, destroySoviets, ObjectiveStatus.Inactive) },
		};
		const int destroySovietsID = 0;
		const string destroySoviets = "Excellent work Commander! We have reinforced our position enough to initiate a counter-attack. Destroy the remaining Soviet forces in the area!";
		const int maintainPresenceID = 1;
		const string maintainPresence = "Commander! The Soviets have rendered us useless... Reports indicate Soviet reinforcements are coming to finish us off... the situation looks bleak...";
	
		Player allies;
		Player soviets;

		Actor sovietEntry1;
		Actor sovietEntry2;
		Actor sovietEntry3;
		CPos[] sovietentrypoints;
		CPos[] newsovietentrypoints;

		Actor sovietrally;
		Actor sovietrally1;
		Actor sovietrally2;
		Actor sovietrally3;
		Actor sovietrally4;
		Actor sovietrally5;
		Actor sovietrally6;
		Actor sovietrally8;
		CPos[] sovietrallypoints;
		CPos[] newsovietrallypoints;
  
		Actor sovietparadrop1;
		Actor sovietparadrop2;
		Actor sovietparadrop3;
		Actor sovietparadropEntry;

		Actor alliesbase;
		Actor factory;
		Actor barrack1;

		Actor drum1;
		Actor drum2;
		Actor drum3;
		Actor FranceEntry;
		Actor FranceRally;
		Actor FranceparaEntry1;
		Actor FranceparaEntry2;
		Actor FranceparaEntry3;

		World world;
		WorldRenderer worldRenderer;

		CountdownTimer survivalTimer;
		CountdownTimerWidget survivalTimerWidget;

		const int timerTicks = 1500 * 10;
		const int attackTicks = 1500 * 1;
		const int sovietAttackGroupSize = 7;
		const int SovietGroupSize = 4;

		const string Camera = "Camera";
		const string InfantryQueueName = "Infantry";
		const string Flare = "flare";

		static readonly string[] FrenchSquad = { "2tnk", "2tnk", "mcv" };
		static readonly string[] SovietInfantry = { "e1", "e4", "e2" };
		static readonly string[] SovietVehicles = { "3tnk", "3tnk", "v2rl" };
		static readonly string[] SovietTanks = { "3tnk", "3tnk", "3tnk" };
		static readonly string[] squad = { "e1", "e1", "e2", "e4", "e4" };
		static readonly string[] platoon = { "e1", "e1", "e2", "e4", "e4", "e1", "e1", "e2", "e4", "e4" };

		int ProduceAtFrame;
		int ProduceAtFrameIncrement;
		int attackAtFrame;
		int attackAtFrameIncrement;

		void MissionAccomplished(string text)
		{
			MissionUtils.CoopMissionAccomplished(world, text, allies);
		}

		void MissionFailed(string text)
		{
			MissionUtils.CoopMissionFailed(world, text, allies);
		}

		void Message(string text)
		{
			Game.AddChatLine(Color.Aqua, "Incoming Report", text);
		}

		void SetSovietUnitsToDefensiveStance()
		{
			foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == soviets && !a.IsDead() && a.HasTrait<AutoTarget>()))
				actor.Trait<AutoTarget>().Stance = UnitStance.Defend;
		}

		Actor FirstUnshroudedOrDefault(IEnumerable<Actor> actors, World world, int shroudRange)
		{
			return actors.FirstOrDefault(u => world.FindAliveCombatantActorsInCircle(u.CenterPosition, WDist.FromCells(shroudRange)).All(a => !a.HasTrait<CreatesShroud>()));
		}

		void AttackNearestAlliedActor(Actor self)
		{
			var enemies = world.Actors.Where(u => u.AppearsHostileTo(self) && (u.Owner == allies)
					&& ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || u.HasTrait<Mobile>()) && u.IsInWorld && !u.IsDead());

			var enemy = FirstUnshroudedOrDefault(enemies.OrderBy(u => (self.CenterPosition - u.CenterPosition).LengthSquared), world, 20);
			if (enemy != null)
				self.QueueActivity(new AttackMove.AttackMoveActivity(self, new Attack(Target.FromActor(enemy), WDist.FromCells(3))));
		}

		void SpawnAndAttack(string[] squad, Player owner, CPos location)
		{
			for (int i = 0; i < squad.Length; i++)
			{
				var actor = world.CreateActor(squad[i], new TypeDictionary { new OwnerInit(owner), new LocationInit(location) });
				AttackNearestAlliedActor(actor);
			}   
		}

		void SpawnFlare(Player owner, Actor location)
		{
			world.CreateActor(Flare, new TypeDictionary { new OwnerInit(owner), new LocationInit(location.Location) });
		}

		void FinalAttack()
		{
			SpawnAndAttack(SovietTanks, soviets, sovietEntry1.Location);
			SpawnAndAttack(SovietTanks, soviets, sovietEntry1.Location);
			SpawnAndAttack(SovietTanks, soviets, sovietEntry2.Location);
			SpawnAndAttack(platoon, soviets, sovietEntry1.Location);
			SpawnAndAttack(platoon, soviets, sovietEntry2.Location);
		}

		void FrenchReinforcements()
		{
			worldRenderer.Viewport.Center(sovietrally1.CenterPosition);
			MissionUtils.Parabomb(world, allies, FranceparaEntry1.Location, drum3.Location);
			MissionUtils.Parabomb(world, allies, FranceparaEntry3.Location, drum2.Location);
			MissionUtils.Parabomb(world, allies, FranceparaEntry2.Location, drum1.Location);
			for (int i = 0; i < FrenchSquad.Length; i++)
			{
				var actor = world.CreateActor(FrenchSquad[i], new TypeDictionary { new OwnerInit(allies), new LocationInit(FranceEntry.Location) });
				actor.QueueActivity(new Move.Move(FranceRally.Location));
			}
		}

		public void Tick(Actor self)
		{
			if (allies.WinState != WinState.Undefined)
				return;

			survivalTimer.Tick();
			if (allies != null)
			{
				ManageSovietUnits();
			}

			var unitsAndBuildings = world.Actors.Where(a => !a.IsDead() && a.IsInWorld && (a.HasTrait<Mobile>() || (a.HasTrait<Building>() && !a.HasTrait<Wall>())));
			if (!unitsAndBuildings.Any(a => a.Owner == soviets))
			{
				objectives[destroySovietsID].Status = ObjectiveStatus.Completed;
				MissionAccomplished("We have destroyed the remaining Soviet presence!");
			}

			if (world.FrameNumber == ProduceAtFrame)
			{
				ProduceAtFrame += ProduceAtFrameIncrement;
				ProduceAtFrameIncrement = Math.Max(ProduceAtFrameIncrement - 5, 100);
				InitializeSovietFactories(barrack1, sovietrally.Location);
				BuildSovietUnits(factory, barrack1);
			}
			if (world.FrameNumber == attackAtFrame)
			{
				attackAtFrame += attackAtFrameIncrement;
				attackAtFrameIncrement = Math.Max(attackAtFrameIncrement - 5, 100);
				ManageSovietVehicles();
				if (producing)
				{
					BuildSovietVehicles(sovietentrypoints, sovietrallypoints);
				}
				else
					BuildSovietVehicles(newsovietentrypoints, newsovietrallypoints);
			}
			if (world.FrameNumber == attackTicks)
			{
				SpawnAndAttack(squad, soviets, sovietrally5.Location);
				SpawnAndAttack(squad, soviets, sovietrally6.Location);
			}
			if (world.FrameNumber == attackTicks * 3)
			{
				SpawnFlare(soviets, sovietparadrop3);
				MissionUtils.Paradrop(world, soviets, squad, sovietparadropEntry.Location, sovietparadrop3.Location);
				SpawnAndAttack(squad, soviets, sovietrally2.Location);
				SpawnAndAttack(platoon, soviets, sovietrally5.Location);
				SpawnAndAttack(platoon, soviets, sovietrally6.Location);
			}
			if (world.FrameNumber == attackTicks * 5)
			{
				SpawnFlare(soviets, sovietparadrop2);
				MissionUtils.Paradrop(world, soviets, squad, sovietparadropEntry.Location, sovietparadrop2.Location);
			}
			if (world.FrameNumber == attackTicks * 7)
			{
				SpawnFlare(soviets, sovietparadrop1);
				MissionUtils.Paradrop(world, soviets, squad, sovietparadropEntry.Location, sovietparadrop1.Location);
			}
			if (world.FrameNumber == attackTicks * 10)
			{
				SpawnFlare(soviets, sovietparadrop1);
				MissionUtils.Paradrop(world, soviets, squad, sovietparadropEntry.Location, sovietparadrop1.Location);
				ManageSovietUnits();
			}
			if (world.FrameNumber == attackTicks * 12)
			{
				Sound.Play("reinfor1.aud");
				FrenchReinforcements();
			}
		}

		void StartCountDownTimer()
		{
			Sound.Play("timergo1.aud");
			survivalTimer = new CountdownTimer(timerTicks, CountDownTimerExpired, true);
			survivalTimerWidget = new CountdownTimerWidget(survivalTimer, "Time Until Soviet Reinforcements Arrive: {0}");
			Ui.Root.AddChild(survivalTimerWidget);
		}

		void CountDownTimerExpired(CountdownTimer countDownTimer)
		{
			survivalTimerWidget.Visible = false;
			Message("The Soviet reinforcements are approuching!");
			BuildSovietVehicles(newsovietentrypoints, newsovietrallypoints);
			FinalAttack();
			producing = false;
			objectives[maintainPresenceID].Status = ObjectiveStatus.Completed;
			objectives[destroySovietsID].Status = ObjectiveStatus.InProgress;
			OnObjectivesUpdated(true);
		}

		void InitializeSovietFactories(Actor tent, CPos rally)
		{
			if (tent.IsInWorld && !tent.IsDead())
			{
				var sbrp = tent.Trait<RallyPoint>();
				sbrp.rallyPoint = rally;
				sbrp.nearEnough = 6;
			}
		}

		void BuildSovietUnit(string category, string unit)
		{
			var queueTent = MissionUtils.FindQueues(world, soviets, category).FirstOrDefault(q => q.CurrentItem() == null);
			if (queueTent == null) return;
			queueTent.ResolveOrder(queueTent.self, Order.StartProduction(queueTent.self, unit, 1));
		}

		void BuildSovietUnits(Actor factory, Actor tent)
		{
			if (barrack1.IsInWorld && !barrack1.IsDead())
			{
				BuildSovietUnit(InfantryQueueName, SovietInfantry.Random(world.SharedRandom));
			}
		}

		void ManageSovietUnits()
		{
			var units = world.FindAliveCombatantActorsInCircle(sovietrally.CenterPosition, WDist.FromCells(3))
					.Where(u => u.IsIdle && u.HasTrait<IPositionable>() && u.HasTrait<AttackBase>() && u.Owner == soviets);
			if (units.Count() >= sovietAttackGroupSize)
			{
				foreach (var unit in units)
				{
					var route = world.SharedRandom.Next(sovietrallypoints.Length);
					unit.QueueActivity(new Move.Move(sovietrally3.Location));
					unit.QueueActivity(new Wait(300));
					unit.QueueActivity(new Move.Move(sovietrallypoints[route]));
					AttackNearestAlliedActor(unit);
				}
			}
		}

		void BuildSovietVehicles(CPos[] spawnpoints, CPos[] rallypoints)
		{
			var route = world.SharedRandom.Next(spawnpoints.Length);
			var spawnPoint = spawnpoints[route];
			var rally = world.SharedRandom.Next(rallypoints.Length);
			var rallyPoint = rallypoints[rally];
			var unit = world.CreateActor(SovietVehicles.Random(world.SharedRandom),
				new TypeDictionary
				{
					new LocationInit(spawnPoint),
					new OwnerInit(soviets)
				});
			unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, new Move.Move(rallyPoint, 3)));
		}

		void ManageSovietVehicles()
		{
			foreach (var rallyPoint in sovietrallypoints)
			{
				var units = world.FindAliveCombatantActorsInCircle(rallyPoint.CenterPosition, WDist.FromCells(10))
					.Where(u => u.IsIdle && u.HasTrait<Mobile>() && u.HasTrait<AttackBase>() && u.Owner == soviets);
				if (units.Count() >= SovietGroupSize)
				{
					foreach (var unit in units)
						AttackNearestAlliedActor(unit);
				}
			}

			var scatteredUnits = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && u.HasTrait<Mobile>() && u.IsIdle && u.Owner == soviets)
				.Except(world.WorldActor.Trait<SpawnMapActors>().Actors.Values)
				.Except(sovietrallypoints.SelectMany(rp => world.FindAliveCombatantActorsInCircle(rp.CenterPosition, WDist.FromCells(10))));

			foreach (var unit in scatteredUnits)
				AttackNearestAlliedActor(unit);
		}
			
		bool producing = true;

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			worldRenderer = wr;

			allies = w.Players.SingleOrDefault(p => p.InternalName == "Allies");
			if (allies != null)
			{
				ProduceAtFrame = 300;
				ProduceAtFrameIncrement = 300;
				attackAtFrame = 450;
				attackAtFrameIncrement = 450;
			}
			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			sovietEntry1 = actors["SovietEntry1"];
			sovietEntry2 = actors["SovietEntry2"];
			sovietEntry3 = actors["SovietEntry3"];
			sovietentrypoints = new[] { sovietEntry1, sovietEntry2, sovietEntry3 }.Select(p => p.Location).ToArray();
			sovietrally = actors["SovietRally"];
			sovietrally1 = actors["SovietRally1"];
			sovietrally2 = actors["SovietRally2"];
			sovietrally3 = actors["SovietRally3"];
			sovietrally4 = actors["SovietRally4"];
			sovietrally5 = actors["SovietRally5"];
			sovietrally6 = actors["SovietRally6"];
			sovietrally8 = actors["SovietRally8"];
			sovietrallypoints = new[] { sovietrally2, sovietrally4, sovietrally5, sovietrally6 }.Select(p => p.Location).ToArray();
			alliesbase = actors["AlliesBase"];
			sovietparadropEntry = actors["SovietParaDropEntry"];
			sovietparadrop1 = actors["SovietParaDrop1"];
			sovietparadrop2 = actors["SovietParaDrop2"];
			sovietparadrop3 = actors["SovietParaDrop3"];
			barrack1 = actors["barrack1"];
			factory = actors["Factory"];
			drum1 = actors["drum1"];
			drum2 = actors["drum2"];
			drum3 = actors["drum3"];
			FranceEntry = actors["FranceEntry"];
			FranceRally = actors["FranceRally"];
			FranceparaEntry1 = actors["FranceparaEntry1"];
			FranceparaEntry2 = actors["FranceparaEntry2"];
			FranceparaEntry3 = actors["FranceparaEntry3"];
			newsovietentrypoints = new[] { sovietparadropEntry, sovietEntry3 }.Select(p => p.Location).ToArray();
			newsovietrallypoints = new[] { sovietrally3, sovietrally4, sovietrally8 }.Select(p => p.Location).ToArray();

			worldRenderer.Viewport.Center(alliesbase.CenterPosition);
			StartCountDownTimer();
			SetSovietUnitsToDefensiveStance();
			world.CreateActor(Camera, new TypeDictionary
			{
				new OwnerInit(allies),
				new LocationInit(sovietrally1.Location),
			});
			MissionUtils.PlayMissionMusic();
		}
	}

}
