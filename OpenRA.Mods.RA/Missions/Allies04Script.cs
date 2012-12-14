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
using OpenRA.Network;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	class Allies04ScriptInfo : TraitInfo<Allies04Script>, Requires<SpawnMapActorsInfo> { }

	class Allies04Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event ObjectivesUpdatedEventHandler OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return objectives.Values; } }

		Dictionary<int, Objective> objectives = new Dictionary<int, Objective>
		{
			{ InfilitrateID, new Objective(ObjectiveType.Primary, Infiltrate, ObjectiveStatus.InProgress) }
		};

		const int InfilitrateID = 0;
		const string Infiltrate = "The Soviets are currently developing a new defensive system named the \"Iron Curtain\" at their main research laboratories. Get our Spy into their main research laboratories.";

		Actor lstEntryPoint;
		Actor lstUnloadPoint;
		Actor lstExitPoint;

		Actor allies1Spy;
		Actor allies2Spy;

		Player allies;
		Player allies1;
		Player allies2;
		Player soviets;
		World world;

		public void Tick(Actor self)
		{
			if (world.FrameNumber == 1)
			{
				InsertSpies();
			}
			PatrolTick();
		}

		void InsertSpies()
		{
			var lst = world.CreateActor("lst", new TypeDictionary 
			{ 
				new OwnerInit(allies1),
				new LocationInit(lstEntryPoint.Location)
			});
			allies1Spy = world.CreateActor(false, "spy", new TypeDictionary { new OwnerInit(allies1) });
			lst.Trait<Cargo>().Load(lst, allies1Spy);
			if (allies1 != allies2)
			{
				allies2Spy = world.CreateActor(false, "spy", new TypeDictionary { new OwnerInit(allies2) });
				lst.Trait<Cargo>().Load(lst, allies2Spy);
			}
			lst.QueueActivity(new Move.Move(lstUnloadPoint.Location));
			lst.QueueActivity(new Wait(10));
			lst.QueueActivity(new UnloadCargo(true));
			lst.QueueActivity(new Wait(10));
			lst.QueueActivity(new Move.Move(lstExitPoint.Location));
			lst.QueueActivity(new RemoveSelf());
		}

		static readonly string[] DogPatrol = { "e1", "dog.patrol", "dog.patrol" };

		IEnumerable<Actor> patrol1;
		CPos[] patrolPoints1;
		int currentPatrolPoint1;

		void PatrolTick()
		{
			if (patrol1 == null)
			{
				var td = new TypeDictionary { new OwnerInit(soviets), new LocationInit(patrolPoints1.First()) };
				patrol1 = DogPatrol.Select(f => world.CreateActor(f, td)).ToArray();
			}
			var leader = patrol1.First();
			if (!leader.IsDead() && leader.IsIdle && leader.IsInWorld)
			{
				currentPatrolPoint1 = (currentPatrolPoint1 + 1) % patrolPoints1.Count();
				leader.QueueActivity(new AttackMove.AttackMoveActivity(leader, new Move.Move(patrolPoints1[currentPatrolPoint1])));
				leader.QueueActivity(new Wait(50));
				foreach (var follower in patrol1.Skip(1))
				{
					follower.QueueActivity(new Wait(world.SharedRandom.Next(0, 25)));
					follower.QueueActivity(new AttackMove.AttackMoveActivity(follower, new Move.Move(patrolPoints1[currentPatrolPoint1])));
				}
			}
		}

		void SetupSubStances()
		{
			if (Game.IsHost)
			{
				foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == soviets && !a.IsDead() && a.HasTrait<TargetableSubmarine>()))
				{
					world.IssueOrder(new Order("SetUnitStance", actor, false)
					{
						TargetLocation = new CPos((int)UnitStance.Defend, 0)
					});
				}
			}
		}

		public void WorldLoaded(World w)
		{
			world = w;
			allies1 = w.Players.Single(p => p.InternalName == "Allies1");
			allies2 = w.Players.SingleOrDefault(p => p.InternalName == "Allies2");
			if (allies2 == null)
			{
				allies2 = allies1;
			}
			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			lstEntryPoint = actors["LstEntryPoint"];
			lstUnloadPoint = actors["LstUnloadPoint"];
			lstExitPoint = actors["LstExitPoint"];
			patrolPoints1 = new[] 
			{
				actors["PatrolPoint11"].Location,
				actors["PatrolPoint12"].Location,
				actors["PatrolPoint13"].Location,
				actors["PatrolPoint14"].Location,
				actors["PatrolPoint15"].Location 
			};
			SetupSubStances();
			Game.MoveViewport(lstEntryPoint.Location.ToFloat2());
			PlayMusic();
			Game.ConnectionStateChanged += StopMusic;
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
