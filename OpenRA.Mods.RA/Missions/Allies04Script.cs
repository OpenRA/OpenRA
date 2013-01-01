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
using System.Drawing;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Render;
using OpenRA.Network;
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
								+ "Get our {0} into the laboratory undetected.";

		Actor lstEntryPoint;
		Actor lstUnloadPoint;
		Actor lstExitPoint;
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

		Player allies;
		Player allies1;
		Player allies2;
		Player soviets;
		World world;

		Patrol[] patrols;
		CPos[] patrolPoints1;
		CPos[] patrolPoints2;
		CPos[] patrolPoints3;
		CPos[] patrolPoints4;
		CPos[] patrolPoints5;

		CPos hind1EntryPoint;
		PPos[] hind1Points;
		CPos hind1ExitPoint;

		Actor reinforcementsEntryPoint;
		Actor reinforcementsUnloadPoint;

		string difficulty;
		int destroyBaseTicks;

		void MissionFailed(string text)
		{
			if (allies1.WinState != WinState.Undefined)
			{
				return;
			}
			allies1.WinState = allies2.WinState = WinState.Lost;
			foreach (var actor in world.Actors.Where(a => a.IsInWorld && (a.Owner == allies1 || a.Owner == allies2) && !a.IsDead()))
			{
				actor.Kill(actor);
			}
			Game.AddChatLine(Color.Red, "Mission failed", text);
			Sound.Play("misnlst1.aud");
		}

		void MissionAccomplished(string text)
		{
			if (allies1.WinState != WinState.Undefined)
			{
				return;
			}
			allies1.WinState = allies2.WinState = WinState.Won;
			Game.AddChatLine(Color.Blue, "Mission accomplished", text);
			Sound.Play("misnwon1.aud");
		}

		public void Tick(Actor self)
		{
			if (allies1.WinState != WinState.Undefined)
			{
				return;
			}
			if (world.FrameNumber == 1)
			{
				InsertSpies();
			}
			if (world.FrameNumber == 600)
			{
				SendHind(hind1EntryPoint, hind1Points, hind1ExitPoint);
			}
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
				{
					destroyBaseTimer.Tick();
				}
			}
			foreach (var patrol in patrols)
			{
				patrol.DoPatrol();
			}
			ManageSovietOre();
			BaseGuardTick();
			if (allies1Spy.IsDead() || (allies2Spy != null && allies2Spy.IsDead()))
			{
				objectives[InfiltrateID].Status = ObjectiveStatus.Failed;
				OnObjectivesUpdated(true);
				MissionFailed("{0} spy was killed.".F(allies1 != allies2 ? "A" : "The"));
			}
			else if (lab.Destroyed)
			{
				MissionFailed("The Soviet research laboratory was destroyed.");
			}
			else if (!world.Actors.Any(a => (a.Owner == allies1 || a.Owner == allies2) && !a.IsDead()
				&& (a.HasTrait<Building>() && !a.HasTrait<Wall>()) || a.HasTrait<BaseBuilding>()))
			{
				objectives[DestroyID].Status = ObjectiveStatus.Failed;
				OnObjectivesUpdated(true);
				MissionFailed("The remaining Allied forces in the area have been wiped out.");
			}
			else if (!world.Actors.Any(a => a.Owner == soviets && a.IsInWorld && !a.IsDead()
				&& a.HasTrait<Building>() && !a.HasTrait<Wall>() && !a.HasTrait<Allies04TrivialBuilding>() && a != lab)
				&& objectives[InfiltrateID].Status == ObjectiveStatus.Completed)
			{
				objectives[DestroyID].Status = ObjectiveStatus.Completed;
				OnObjectivesUpdated(true);
				MissionAccomplished("The Soviet research laboratory has been secured successfully.");
			}
		}

		void OnDestroyBaseTimerExpired(CountdownTimer t)
		{
			if (!world.Actors.Any(a => a.Owner == soviets && a.IsInWorld && !a.IsDead()
				&& a.HasTrait<Building>() && !a.HasTrait<Wall>() && !a.HasTrait<Allies04TrivialBuilding>() && a != lab)
				&& objectives[InfiltrateID].Status == ObjectiveStatus.Completed)
			{
				return;
			}
			objectives[DestroyID].Status = ObjectiveStatus.Failed;
			OnObjectivesUpdated(true);
			MissionFailed("The Soviet research laboratory was not secured in time.");
		}

		void ManageSovietOre()
		{
			var res = soviets.PlayerActor.Trait<PlayerResources>();
			if (res.Ore > res.OreCapacity * 0.8)
			{
				res.TakeOre(res.OreCapacity / 10);
			}
		}

		void BaseGuardTick()
		{
			if (baseGuardTicks <= 0 || baseGuard.IsDead() || !baseGuard.IsInWorld)
			{
				return;
			}
			if (hijackTruck.Location == baseGuardTruckPos.Location)
			{
				if (--baseGuardTicks <= 0)
				{
					baseGuard.QueueActivity(new Move.Move(baseGuardMovePos.Location));
				}
			}
			else
			{
				baseGuardTicks = 100;
			}
		}

		void OnLabInfiltrated(Actor spy)
		{
			if (spy == allies1Spy) { allies1SpyInfiltratedLab = true; }
			else if (spy == allies2Spy) { allies2SpyInfiltratedLab = true; }
			if (allies1SpyInfiltratedLab && (allies2SpyInfiltratedLab || allies2Spy == null))
			{
				objectives[InfiltrateID].Status = ObjectiveStatus.Completed;
				objectives[DestroyID].Status = ObjectiveStatus.InProgress;
				OnObjectivesUpdated(true);
				frameInfiltrated = world.FrameNumber;
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
			{
				lst.Trait<Cargo>().Load(lst, world.CreateActor(false, "mcv", new TypeDictionary { new OwnerInit(allies2) }));
			}
			lst.QueueActivity(new Move.Move(reinforcementsUnloadPoint.Location));
			lst.QueueActivity(new Wait(10));
			lst.QueueActivity(new UnloadCargo(true));
			lst.QueueActivity(new Wait(10));
			lst.QueueActivity(new Move.Move(reinforcementsEntryPoint.Location));
			lst.QueueActivity(new RemoveSelf());
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
				if (actors.Any(a => a.IsDead() || !a.IsIdle || !a.IsInWorld))
				{
					return;
				}
				pointIndex = (pointIndex + 1) % points.Length;
				foreach (var actor in actors.Where(a => !a.IsDead() && a.IsInWorld))
				{
					actor.QueueActivity(new Wait(world.SharedRandom.Next(50, 75)));
					actor.QueueActivity(new AttackMove.AttackMoveActivity(actor, new Move.Move(points[pointIndex], 0)));
				}
			}
		}

		void SendHind(CPos start, IEnumerable<PPos> points, CPos exit)
		{
			var hind = world.CreateActor("hind.autotarget", new TypeDictionary
			{
				new OwnerInit(soviets),
				new LocationInit(start),
				new FacingInit(Util.GetFacing(points.First().ToCPos() - start, 0)),
				new AltitudeInit(Rules.Info["hind.autotarget"].Traits.Get<HelicopterInfo>().CruiseAltitude),
			});
			foreach (var point in points.Concat(new[] { Util.CenterOfCell(exit) }))
			{
				hind.QueueActivity(new AttackMove.AttackMoveActivity(hind, new HeliFly(point)));
			}
			hind.QueueActivity(new RemoveSelf());
		}

		void InsertSpies()
		{
			var lst = world.CreateActor("lst.unselectable", new TypeDictionary 
			{ 
				new OwnerInit(allies1),
				new LocationInit(lstEntryPoint.Location)
			});
			allies1Spy = world.CreateActor(false, "spy.strong", new TypeDictionary { new OwnerInit(allies1) });
			lst.Trait<Cargo>().Load(lst, allies1Spy);
			if (allies1 != allies2)
			{
				allies2Spy = world.CreateActor(false, "spy.strong", new TypeDictionary { new OwnerInit(allies2) });
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

			difficulty = w.LobbyInfo.GlobalSettings.Difficulty;
			Game.Debug("{0} difficulty selected".F(difficulty));

			allies1 = w.Players.Single(p => p.InternalName == "Allies1");
			allies2 = w.Players.SingleOrDefault(p => p.InternalName == "Allies2");
			if (allies2 == null)
			{
				allies2 = allies1;
			}
			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");

			destroyBaseTicks = difficulty == "Hard" ? 1500 * 20 : difficulty == "Normal" ? 1500 * 25 : 1500 * 30;

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
			lab = actors["Lab"];
			lab.AddTrait(new Allies04InfiltrateAction(OnLabInfiltrated));
			hind1EntryPoint = actors["Hind1EntryPoint"].Location;
			hind1Points = new[]
			{
				actors["Hind1Point1"].CenterLocation,
				actors["Hind1Point2"].CenterLocation
			};
			hind1ExitPoint = actors["Hind1ExitPoint"].Location;
			reinforcementsEntryPoint = actors["ReinforcementsEntryPoint"];
			reinforcementsUnloadPoint = actors["ReinforcementsUnloadPoint"];
			patrols = new[]
			{
				new Patrol(world, new[]{ "e1", "e1", "e1", "e1", "e1" }, soviets, patrolPoints1, 0),
				new Patrol(world, new[]{ "e1", "dog.patrol", "dog.patrol" }, soviets, patrolPoints2, 3),
				new Patrol(world, new[]{ "e1", "dog.patrol", "dog.patrol" }, soviets, patrolPoints3, 0),
				new Patrol(world, new[]{ "e1", "dog.patrol", "dog.patrol" }, soviets, patrolPoints4, 0),
				new Patrol(world, new[]{ "e1", "dog.patrol", "dog.patrol" }, soviets, patrolPoints5, 0),
			};
			objectives[InfiltrateID].Text = Infiltrate.F(allies1 != allies2 ? "spies" : "spy");
			OnObjectivesUpdated(false);
			SetupSubStances();
			Game.MoveViewport(lstEntryPoint.Location.ToFloat2());
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

	class Allies04TryRepairBuildingInfo : ITraitInfo
	{
		public readonly string Player;

		public object Create(ActorInitializer init) { return new Allies04TryRepairBuilding(this); }
	}

	class Allies04TryRepairBuilding : INotifyDamageStateChanged
	{
		Allies04TryRepairBuildingInfo info;

		public Allies04TryRepairBuilding(Allies04TryRepairBuildingInfo info)
		{
			this.info = info;
		}

		public void DamageStateChanged(Actor self, AttackInfo e)
		{
			if (self.HasTrait<RepairableBuilding>() && self.Owner.InternalName == info.Player && Game.IsHost
				&& e.DamageState > DamageState.Undamaged && e.PreviousDamageState == DamageState.Undamaged)
			{
				self.World.IssueOrder(new Order("RepairBuilding", self.Owner.PlayerActor, false) { TargetActor = self });
			}
		}
	}
}
