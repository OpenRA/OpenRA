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
using OpenRA.Mods.RA.Render;
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
		Actor hijackTruck;
		Actor baseGuard;
		Actor baseGuardMovePos;
		Actor baseGuardTruckPos;
		int baseGuardWait = 100;
		bool baseGuardMoved;

		Actor allies1Spy;
		Actor allies2Spy;

		Player allies;
		Player allies1;
		Player allies2;
		Player soviets;
		World world;

		static readonly string[] DogPatrol = { "e1", "dog.patrol", "dog.patrol" };
		static readonly string[] InfantryPatrol = { "e1", "e1", "e1", "e1", "e1" };

		Actor[] patrol1;
		CPos[] patrolPoints1;
		int currentPatrolPoint1;
		Actor[] patrol2;
		CPos[] patrolPoints2;
		int currentPatrolPoint2 = 3;
		Actor[] patrol3;
		CPos[] patrolPoints3;
		int currentPatrolPoint3;
		Actor[] patrol4;
		CPos[] patrolPoints4;
		int currentPatrolPoint4;
		Actor[] patrol5;
		CPos[] patrolPoints5;
		int currentPatrolPoint5;

		public void Tick(Actor self)
		{
			if (world.FrameNumber == 1)
			{
				InsertSpies();
			}
			PatrolTick(ref patrol1, ref currentPatrolPoint1, soviets, DogPatrol, patrolPoints1);
			PatrolTick(ref patrol2, ref currentPatrolPoint2, soviets, InfantryPatrol, patrolPoints2);
			PatrolTick(ref patrol3, ref currentPatrolPoint3, soviets, DogPatrol, patrolPoints3);
			PatrolTick(ref patrol4, ref currentPatrolPoint4, soviets, DogPatrol, patrolPoints4);
			PatrolTick(ref patrol5, ref currentPatrolPoint5, soviets, DogPatrol, patrolPoints5);
			ManageSovietOre();
			BaseGuardTick();
		}

		void ManageSovietOre()
		{
			var res = soviets.PlayerActor.Trait<PlayerResources>();
			res.TakeOre(res.Ore);
			res.TakeCash(res.Cash);
		}

		void BaseGuardTick()
		{
			if (!baseGuardMoved && !baseGuard.IsDead() && baseGuard.IsInWorld)
			{
				if (hijackTruck.Location == baseGuardTruckPos.Location)
				{
					if (--baseGuardWait <= 0)
					{
						baseGuard.QueueActivity(new Move.Move(baseGuardMovePos.Location));
						baseGuardMoved = true;
					}
				}
				else
				{
					baseGuardWait = 100;
				}
			}
		}

		void PatrolTick(ref Actor[] patrolActors, ref int currentPoint, Player owner, string[] actorNames, CPos[] points)
		{
			if (patrolActors == null)
			{
				var td = new TypeDictionary { new OwnerInit(owner), new LocationInit(points[currentPoint]) };
				patrolActors = actorNames.Select(f => world.CreateActor(f, td)).ToArray();
			}
			var leader = patrolActors[0];
			if (!leader.IsDead() && leader.IsIdle && leader.IsInWorld)
			{
				currentPoint = (currentPoint + 1) % points.Length;
				leader.QueueActivity(new AttackMove.AttackMoveActivity(leader, new Move.Move(points[currentPoint], 0)));
				leader.QueueActivity(new Wait(50));
				foreach (var follower in patrolActors.Skip(1))
				{
					follower.QueueActivity(new Wait(world.SharedRandom.Next(0, 25)));
					follower.QueueActivity(new AttackMove.AttackMoveActivity(follower, new Move.Move(points[currentPoint], 0)));
				}
			}
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

		void SetupSubStances()
		{
			if (!Game.IsHost)
			{
				return;
			}
			foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == soviets && !a.IsDead() && a.HasTrait<TargetableSubmarine>()))
			{
				world.IssueOrder(new Order("SetUnitStance", actor, false)
				{
					TargetLocation = new CPos((int)UnitStance.Defend, 0)
				});
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
			hijackTruck = actors["HijackTruck"];
			baseGuard = actors["BaseGuard"];
			baseGuardMovePos = actors["BaseGuardMovePos"];
			baseGuardTruckPos = actors["BaseGuardTruckPos"];
			patrolPoints1 = new[] 
			{
				actors["PatrolPoint11"].Location,
				actors["PatrolPoint12"].Location,
				actors["PatrolPoint13"].Location,
				actors["PatrolPoint14"].Location,
				actors["PatrolPoint15"].Location
			};
			patrolPoints2 = patrolPoints1;
			patrolPoints3 = new[] 
			{
				actors["PatrolPoint21"].Location,
				actors["PatrolPoint22"].Location,
				actors["PatrolPoint23"].Location,
				actors["PatrolPoint24"].Location,
				actors["PatrolPoint25"].Location
			};
			patrolPoints4 = new[] 
			{
				actors["PatrolPoint31"].Location,
				actors["PatrolPoint32"].Location,
				actors["PatrolPoint33"].Location,
				actors["PatrolPoint34"].Location
			};
			patrolPoints5 = new[] 
			{
				actors["PatrolPoint41"].Location,
				actors["PatrolPoint42"].Location,
				actors["PatrolPoint43"].Location,
				actors["PatrolPoint44"].Location,
				actors["PatrolPoint45"].Location
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

	class Allies04HijackableInfo : ITraitInfo
	{
		public object Create(ActorInitializer init) { return new Allies04Hijackable(init.self); }
	}

	class Allies04Hijackable : IAcceptSpy, INotifyPassengerExited
	{
		public Player OldOwner;

		public Allies04Hijackable(Actor self)
		{
			OldOwner = self.Owner;
		}

		public void OnInfiltrate(Actor self, Actor spy)
		{
			if (self.Trait<Cargo>().IsEmpty(self))
			{
				self.ChangeOwner(spy.Owner);
			}
			self.Trait<Cargo>().Load(self, spy);
		}

		public void PassengerExited(Actor self, Actor passenger)
		{
			if (self.Trait<Cargo>().IsEmpty(self))
			{
				self.CancelActivity();
				self.ChangeOwner(OldOwner);
			}
			else if (self.Owner == passenger.Owner)
			{
				self.ChangeOwner(self.Trait<Cargo>().Passengers.First().Owner);
			}
		}
	}

	class Allies04RenderHijackedInfo : RenderUnitInfo
	{
		public override object Create(ActorInitializer init) { return new Allies04RenderHijacked(init.self, this); }
	}

	class Allies04RenderHijacked : RenderUnit, IRenderModifier
	{
		Allies04Hijackable hijackable;

		public Allies04RenderHijacked(Actor self, Allies04RenderHijackedInfo info)
			: base(self)
		{
			hijackable = self.Trait<Allies04Hijackable>();
		}

		public IEnumerable<Renderable> ModifyRender(Actor self, IEnumerable<Renderable> r)
		{
			return r.Select(a => a.WithPalette(Palette(hijackable.OldOwner)));
		}
	}
}
