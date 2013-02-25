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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Missions
{
	class Allies04ScriptInfo : TraitInfo<Allies04Script>, Requires<SpawnMapActorsInfo> { }

	class Allies04Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event ObjectivesUpdatedEventHandler OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return objectives.Values; } }

		Dictionary<int, Objective> objectives = new Dictionary<int, Objective>
		{
			{ InfiltrateID, new Objective(ObjectiveType.Primary, "", ObjectiveStatus.InProgress) },
			{ DestroyID, new Objective(ObjectiveType.Primary, Destroy, ObjectiveStatus.Inactive) }
		};

		const int InfiltrateID = 0;
		const int DestroyID = 1;
		const string Destroy = "Secure the laboratory and destroy the rest of the Soviet base. Ensure that the laboratory is not destroyed.";
		const string Infiltrate = "The Soviets are currently developing a new defensive system named the \"Iron Curtain\" at their main research laboratory."
								+ " Get our {0} into the laboratory undetected.";

		Actor hijackTruck;
		Actor baseGuard;
		Actor baseGuardMovePos;
		Actor baseGuardTruckPos;
		Actor lab;
		int baseGuardTicks = 100;

		Actor allies1Spy;
		Actor allies2Spy;
		bool allies1SpyInfiltratedLab;
		bool allies2SpyInfiltratedLab;
		int frameInfiltrated = -1;

		CountdownTimer destroyBaseTimer;
		CountdownTimerWidget destroyBaseTimerWidget;

		Player allies1;
		Player allies2;
		Player soviets;
		Player neutral;
		World world;

		List<Patrol> patrols;
		CPos[] patrolPoints1;
		CPos[] patrolPoints2;
		CPos[] patrolPoints3;
		CPos[] patrolPoints4;
		CPos[] patrolPoints5;

		Actor reinforcementsEntryPoint;
		Actor reinforcementsUnloadPoint;

		Actor spyReinforcementsEntryPoint;
		Actor spyReinforcementsUnloadPoint;
		Actor spyReinforcementsExitPoint;

		string difficulty;
		int destroyBaseTicks;

		int nextCivilianMove = 1;

		Actor bridgeTank;
		Actor bridgeAttackPoint;
		Actor bridge;
		bool attackingBridge;

		bool attackingTown = true;
		Actor[] townAttackers;

		void MissionFailed(string text)
		{
			MissionUtils.CoopMissionFailed(world, text, allies1, allies2);
		}

		void MissionAccomplished(string text)
		{
			MissionUtils.CoopMissionAccomplished(world, text, allies1, allies2);
		}

		public void Tick(Actor self)
		{
			if (allies1.WinState != WinState.Undefined) return;

			if (world.FrameNumber == 1)
				InsertSpies();

			if (frameInfiltrated != -1)
			{
				if (world.FrameNumber == frameInfiltrated + 100)
				{
					Sound.Play("aarrivs1.aud");
					Game.MoveViewport(reinforcementsUnloadPoint.Location.ToFloat2());
					world.AddFrameEndTask(w => SendReinforcements());
				}
				if (world.FrameNumber == frameInfiltrated + 200)
				{
					Sound.Play("timergo1.aud");
					destroyBaseTimer = new CountdownTimer(destroyBaseTicks, OnDestroyBaseTimerExpired, true);
					destroyBaseTimerWidget = new CountdownTimerWidget(destroyBaseTimer, "Secure lab in: {0}");
					Ui.Root.AddChild(destroyBaseTimerWidget);
				}

				if (world.FrameNumber >= frameInfiltrated + 200)
					destroyBaseTimer.Tick();

				if (world.FrameNumber == frameInfiltrated + 1500 * 12 && !bridgeTank.IsDead() && bridgeTank.IsInWorld && !bridge.IsDead())
				{
					bridgeTank.QueueActivity(new Attack(Target.FromPos(bridge.CenterLocation), 4));
					attackingBridge = true;
				}
				if (attackingBridge && bridge.IsDead())
				{
					if (!bridgeTank.IsDead())
						bridgeTank.CancelActivity();
					attackingBridge = false;
				}

				if (world.FrameNumber == frameInfiltrated + 1500 * 6)
					foreach (var attacker in townAttackers.Where(a => !a.IsDead() && a.IsInWorld))
					{
						attacker.CancelActivity();
						attacker.QueueActivity(new AttackMove.AttackMoveActivity(attacker, new Move.Move(reinforcementsUnloadPoint.Location + new CVec(10, -15), 3)));
					}
			}

			if (attackingTown)
			{
				foreach (var attacker in townAttackers.Where(u => u.IsIdle && !u.IsDead() && u.IsInWorld))
				{
					var enemies = world.Actors.Where(u => u.Owner == neutral && u.HasTrait<ITargetable>()
						&& ((u.HasTrait<Building>() && !u.HasTrait<Wall>() && !u.HasTrait<Bridge>()) || u.HasTrait<Mobile>()) && !u.IsDead() && u.IsInWorld);

					var enemy = enemies.OrderBy(u => (attacker.CenterLocation - u.CenterLocation).LengthSquared).FirstOrDefault();
					if (enemy != null)
						attacker.QueueActivity(new AttackMove.AttackMoveActivity(attacker, new Attack(Target.FromActor(enemy), 3)));
					else
					{
						attackingTown = false;
						break;
					}
				}
			}

			foreach (var patrol in patrols)
				patrol.DoPatrol();

			ManageSovietOre();

			BaseGuardTick();

			if (allies1Spy.IsDead() || (allies2Spy != null && allies2Spy.IsDead()))
			{
				objectives[InfiltrateID].Status = ObjectiveStatus.Failed;
				OnObjectivesUpdated(true);
				MissionFailed("{0} spy was killed.".F(allies1 != allies2 ? "A" : "The"));
			}
			else if (lab.Destroyed)
				MissionFailed("The Soviet research laboratory was destroyed.");
			else if (!world.Actors.Any(a => (a.Owner == allies1 || a.Owner == allies2) && !a.IsDead()
				&& (a.HasTrait<Building>() && !a.HasTrait<Wall>()) || a.HasTrait<BaseBuilding>()))
			{
				objectives[DestroyID].Status = ObjectiveStatus.Failed;
				OnObjectivesUpdated(true);
				MissionFailed("The remaining Allied forces in the area have been wiped out.");
			}
			else if (SovietBaseDestroyed() && objectives[InfiltrateID].Status == ObjectiveStatus.Completed)
			{
				objectives[DestroyID].Status = ObjectiveStatus.Completed;
				OnObjectivesUpdated(true);
				MissionAccomplished("The Soviet research laboratory has been secured successfully.");
			}
			if (world.FrameNumber == nextCivilianMove)
			{
				var civilians = world.Actors.Where(a => !a.IsDead() && a.IsInWorld && a.Owner == neutral && a.HasTrait<Mobile>());
				if (civilians.Any())
				{
					var civilian = civilians.Random(world.SharedRandom);
					civilian.Trait<Mobile>().Nudge(civilian, civilian, true);
					nextCivilianMove += world.SharedRandom.Next(1, 75);
				}
			}
		}

		bool SovietBaseDestroyed()
		{
			return !world.Actors.Any(a => a.Owner == soviets && a.IsInWorld && !a.IsDead()
				&& a.HasTrait<Building>() && !a.HasTrait<Wall>() && !a.HasTrait<Allies04TrivialBuilding>() && a != lab);
		}

		void OnDestroyBaseTimerExpired(CountdownTimer t)
		{
			if (SovietBaseDestroyed() && objectives[InfiltrateID].Status == ObjectiveStatus.Completed) return;
			objectives[DestroyID].Status = ObjectiveStatus.Failed;
			OnObjectivesUpdated(true);
			MissionFailed("The Soviet research laboratory was not secured in time.");
		}

		void ManageSovietOre()
		{
			var res = soviets.PlayerActor.Trait<PlayerResources>();
			if (res.Ore > res.OreCapacity * 0.8)
				res.TakeOre(res.OreCapacity / 10);
		}

		void BaseGuardTick()
		{
			if (baseGuardTicks <= 0 || baseGuard.IsDead() || !baseGuard.IsInWorld) return;

			if (hijackTruck.Location == baseGuardTruckPos.Location)
			{
				if (--baseGuardTicks <= 0)
					baseGuard.QueueActivity(new Move.Move(baseGuardMovePos.Location));
			}
			else
				baseGuardTicks = 100;
		}

		void OnLabInfiltrated(Actor spy)
		{
			if (spy == allies1Spy) allies1SpyInfiltratedLab = true;
			else if (spy == allies2Spy) allies2SpyInfiltratedLab = true;

			if (allies1SpyInfiltratedLab && (allies2SpyInfiltratedLab || allies2Spy == null))
			{
				objectives[InfiltrateID].Status = ObjectiveStatus.Completed;
				objectives[DestroyID].Status = ObjectiveStatus.InProgress;
				OnObjectivesUpdated(true);
				frameInfiltrated = world.FrameNumber;

				foreach (var actor in world.Actors.Where(a => !a.IsDead() && a.HasTrait<Allies04TransformOnLabInfiltrate>()))
					actor.QueueActivity(false, new Transform(actor, actor.Info.Traits.Get<Allies04TransformOnLabInfiltrateInfo>().ToActor));
			}
		}

		void SendReinforcements()
		{
			var lst = world.CreateActor("lst.unselectable", new TypeDictionary 
			{ 
				new OwnerInit(allies1),
				new LocationInit(reinforcementsEntryPoint.Location)
			});
			lst.Trait<Cargo>().Load(lst, world.CreateActor(false, "mcv", new TypeDictionary { new OwnerInit(allies1) }));
			if (allies1 != allies2)
				lst.Trait<Cargo>().Load(lst, world.CreateActor(false, "mcv", new TypeDictionary { new OwnerInit(allies2) }));

			lst.QueueActivity(new Move.Move(reinforcementsUnloadPoint.Location));
			lst.QueueActivity(new Wait(10));

			lst.QueueActivity(new CallFunc(() =>
			{
				allies1.PlayerActor.Trait<PlayerResources>().GiveCash(allies1 == allies2 ? 5000 : 2500);
				if (allies1 != allies2)
					allies2.PlayerActor.Trait<PlayerResources>().GiveCash(2500);
			}));

			lst.AddTrait(new Allies04TransformedAction(self =>
			{
				self.QueueActivity(new Wait(10));
				self.QueueActivity(new Move.Move(reinforcementsEntryPoint.Location));
				self.QueueActivity(new RemoveSelf());
			}));
			lst.QueueActivity(new UnloadCargo(true));
			lst.QueueActivity(new Transform(lst, "lst.unselectable.nocargo") { SkipMakeAnims = true });
		}

		class Patrol
		{
			Actor[] actors;
			CPos[] points;
			int pointIndex;
			World world;

			public Patrol(World world, string[] actorNames, Player owner, CPos[] points, int pointIndex)
			{
				this.world = world;
				this.points = points;
				this.pointIndex = pointIndex;
				var td = new TypeDictionary { new OwnerInit(owner), new LocationInit(points[pointIndex]) };
				this.actors = actorNames.Select(a => world.CreateActor(a, td)).ToArray();
			}

			public void DoPatrol()
			{
				if (actors.Any(a => a.IsDead() || !a.IsIdle || !a.IsInWorld)) return;

				pointIndex = (pointIndex + 1) % points.Length;
				foreach (var actor in actors.Where(a => !a.IsDead() && a.IsInWorld))
				{
					actor.QueueActivity(new Wait(world.SharedRandom.Next(50, 75)));
					actor.QueueActivity(new AttackMove.AttackMoveActivity(actor, new Move.Move(points[pointIndex], 0)));
				}
			}
		}

		void InsertSpies()
		{
			var lst = world.CreateActor("lst.unselectable", new TypeDictionary 
			{ 
				new OwnerInit(allies1),
				new LocationInit(spyReinforcementsEntryPoint.Location)
			});

			allies1Spy = world.CreateActor(false, "spy.strong", new TypeDictionary { new OwnerInit(allies1) });
			lst.Trait<Cargo>().Load(lst, allies1Spy);
			if (allies1 != allies2)
			{
				allies2Spy = world.CreateActor(false, "spy.strong", new TypeDictionary { new OwnerInit(allies2) });
				lst.Trait<Cargo>().Load(lst, allies2Spy);
			}

			lst.AddTrait(new Allies04TransformedAction(self =>
			{
				self.QueueActivity(new Wait(10));
				self.QueueActivity(new Move.Move(spyReinforcementsExitPoint.Location));
				self.QueueActivity(new RemoveSelf());
			}));
			lst.QueueActivity(new Move.Move(spyReinforcementsUnloadPoint.Location));
			lst.QueueActivity(new Wait(10));
			lst.QueueActivity(new UnloadCargo(true));
			lst.QueueActivity(new Transform(lst, "lst.unselectable.nocargo") { SkipMakeAnims = true });
		}

		void SetupSubStances()
		{
			foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == soviets && !a.IsDead() && a.HasTrait<TargetableSubmarine>()))
				actor.Trait<AutoTarget>().stance = UnitStance.Defend;
		}

		public void WorldLoaded(World w)
		{
			world = w;

			difficulty = w.LobbyInfo.GlobalSettings.Difficulty;
			Game.Debug("{0} difficulty selected".F(difficulty));

			allies1 = w.Players.Single(p => p.InternalName == "Allies1");
			allies2 = w.Players.SingleOrDefault(p => p.InternalName == "Allies2");

			allies1.PlayerActor.Trait<PlayerResources>().Cash = 0;
			if (allies2 == null)
				allies2 = allies1;
			else
			{
				allies2.PlayerActor.Trait<PlayerResources>().Cash = 0;
			}

			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			neutral = w.Players.Single(p => p.InternalName == "Neutral");
			objectives[InfiltrateID].Text = Infiltrate.F(allies1 != allies2 ? "spies" : "spy");

			destroyBaseTicks = difficulty == "Hard" ? 1500 * 25 : difficulty == "Normal" ? 1500 * 28 : 1500 * 31;

			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;

			spyReinforcementsEntryPoint = actors["SpyReinforcementsEntryPoint"];
			spyReinforcementsUnloadPoint = actors["SpyReinforcementsUnloadPoint"];
			spyReinforcementsExitPoint = actors["SpyReinforcementsExitPoint"];

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
			lab = actors["Lab"];
			lab.AddTrait(new Allies04InfiltrateAction(OnLabInfiltrated));

			reinforcementsEntryPoint = actors["ReinforcementsEntryPoint"];
			reinforcementsUnloadPoint = actors["ReinforcementsUnloadPoint"];

			patrols = new List<Patrol>
			{
				new Patrol(world, new[] { "e1", "e1", "e1", "e1", "e1" }, soviets, patrolPoints1, 0),
				new Patrol(world, new[] { "e1", "dog.patrol", "dog.patrol" }, soviets, patrolPoints2, 2),
				new Patrol(world, new[] { "e1", "dog.patrol", "dog.patrol" }, soviets, patrolPoints4, 0),
				new Patrol(world, new[] { "e1", "dog.patrol", "dog.patrol" }, soviets, patrolPoints5, 0),
			};
			if (difficulty == "Hard")
				patrols.Add(new Patrol(world, new[] { "e1", "e1", "dog.patrol" }, soviets, patrolPoints3, 0));

			bridgeTank = actors["BridgeTank"];
			bridgeAttackPoint = actors["BridgeAttackPoint"];
			bridge = world.Actors
				.Where(a => a.HasTrait<Bridge>() && !a.IsDead())
				.OrderBy(a => (a.Location - bridgeAttackPoint.Location).LengthSquared)
				.FirstOrDefault();

			var ta1 = actors["TownAttacker1"];
			var ta2 = actors["TownAttacker2"];
			var ta3 = actors["TownAttacker3"];
			var ta4 = actors["TownAttacker4"];
			var ta5 = actors["TownAttacker5"];
			var ta6 = actors["TownAttacker6"];
			var ta7 = actors["TownAttacker7"];

			townAttackers = new[] { ta1, ta2, ta3, ta4, ta5, ta6, ta7 };

			OnObjectivesUpdated(false);
			SetupSubStances();

			Game.MoveViewport(spyReinforcementsEntryPoint.Location.ToFloat2());
			MissionUtils.PlayMissionMusic();
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
				self.ChangeOwner(spy.Owner);

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
				self.ChangeOwner(self.Trait<Cargo>().Passengers.First().Owner);
		}
	}

	class Allies04RenderHijackedInfo : RenderUnitInfo
	{
		public override object Create(ActorInitializer init) { return new Allies04RenderHijacked(init.self, this); }
	}

	class Allies04RenderHijacked : RenderUnit
	{
		Allies04Hijackable hijackable;
		Allies04RenderHijackedInfo info;

		public Allies04RenderHijacked(Actor self, Allies04RenderHijackedInfo info)
			: base(self)
		{
			this.info = info;
			hijackable = self.Trait<Allies04Hijackable>();
		}

		protected override string PaletteName(Actor self)
		{
			return info.Palette ?? info.PlayerPalette + hijackable.OldOwner.InternalName;
		}
	}

	class Allies04InfiltrateAction : IAcceptSpy
	{
		Action<Actor> a;

		public Allies04InfiltrateAction(Action<Actor> a)
		{
			this.a = a;
		}

		public void OnInfiltrate(Actor self, Actor spy)
		{
			a(spy);
		}
	}

	class Allies04TrivialBuildingInfo : TraitInfo<Allies04TrivialBuilding> { }
	class Allies04TrivialBuilding { }

	class Allies04MaintainBuildingInfo : TraitInfo<Allies04MaintainBuilding>
	{
		public readonly string Player = null;
	}

	class Allies04MaintainBuilding : INotifyDamageStateChanged
	{
		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (self.Owner.InternalName != self.Info.Traits.Get<Allies04MaintainBuildingInfo>().Player) return;

			if (self.HasTrait<Sellable>() && e.DamageState == DamageState.Critical && e.PreviousDamageState < DamageState.Critical)
				self.Trait<Sellable>().Sell(self);

			else if (self.HasTrait<RepairableBuilding>() && e.DamageState > DamageState.Undamaged && e.PreviousDamageState == DamageState.Undamaged)
				self.Trait<RepairableBuilding>().RepairBuilding(self, self.Owner);
		}
	}

	class Allies04TransformOnLabInfiltrateInfo : TraitInfo<Allies04TransformOnLabInfiltrate>
	{
		[ActorReference]
		public readonly string ToActor = null;
	}

	class Allies04TransformOnLabInfiltrate { }

	class Allies04TransformedAction : INotifyTransformed
	{
		Action<Actor> a;

		public Allies04TransformedAction(Action<Actor> a)
		{
			this.a = a;
		}

		public void OnTransformed(Actor toActor)
		{
			a(toActor);
		}
	}
}
