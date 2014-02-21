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
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Missions
{
	class Allies03ScriptInfo : TraitInfo<Allies03Script>, Requires<SpawnMapActorsInfo> { }

	class Allies03Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event Action<bool> OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return new[] { evacuateUnits, destroyAirbases, evacuateMgg }; } }

		Objective evacuateUnits = new Objective(ObjectiveType.Primary, EvacuateUnitsText, ObjectiveStatus.InProgress);
		Objective destroyAirbases = new Objective(ObjectiveType.Secondary, DestroyAirbasesText, ObjectiveStatus.InProgress);
		Objective evacuateMgg = new Objective(ObjectiveType.Secondary, EvacuateMggText, ObjectiveStatus.InProgress);

		const string EvacuateUnitsText = "Following the rescue of Einstein, the Allies are now being flanked from both sides."
									+ " Evacuate {0} units before the remaining Allied forces in the area are wiped out.";
		const string DestroyAirbasesText = "Destroy the nearby Soviet airbases.";
		const string EvacuateMggText = "Einstein has recently developed a technology which allows us to obscure units from the enemy."
									+ " Evacuate at least one prototype mobile gap generator intact.";

		const string ShortEvacuateTemplate = "{0}/{1} units evacuated";

		int unitsEvacuatedThreshold;
		int unitsEvacuated;
		InfoWidget evacuateWidget;

		World world;
		Player allies1;
		Player allies2;
		Player allies;
		Player soviets;

		Actor exit1TopLeft;
		Actor exit1BottomRight;
		Actor exit1ExitPoint;

		Actor exit2TopLeft;
		Actor exit2BottomRight;
		Actor exit2ExitPoint;

		Actor sovietEntryPoint1;
		Actor sovietEntryPoint2;
		Actor sovietEntryPoint3;
		Actor sovietEntryPoint4;
		Actor sovietEntryPoint5;
		Actor sovietEntryPoint6;
		CPos[] sovietEntryPoints;
		Actor sovietRallyPoint1;
		Actor sovietRallyPoint2;
		Actor sovietRallyPoint3;
		Actor sovietRallyPoint4;
		Actor sovietRallyPoint5;
		Actor sovietRallyPoint6;
		CPos[] sovietRallyPoints;

		Actor[] sovietAirfields;

		Rectangle paradropBox;

		const int ReinforcementsTicks1 = 1500 * 5;
		static readonly string[] Reinforcements1 = { "mgg", "2tnk", "2tnk", "2tnk", "2tnk", "1tnk", "1tnk", "jeep", "jeep", "e1", "e1", "e1", "e1", "e3", "e3" };
		int currentReinforcement1;

		const int ReinforcementsTicks2 = 1500 * 10;
		static readonly string[] Reinforcements2 = { "mgg", "2tnk", "2tnk", "2tnk", "2tnk", "truk", "truk", "truk", "truk", "truk", "truk", "1tnk", "1tnk", "jeep", "jeep" };
		int currentReinforcement2;

		static readonly string[] SovietUnits1 = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "ftrk", "apc", "e1", "e1", "e2", "e3", "e3", "e4" };
		static readonly string[] SovietUnits2 = { "4tnk", "4tnk", "4tnk", "4tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "ftrk", "apc", "e1", "e1", "e2", "e3", "e3", "e4" };
		int sovietUnits2Ticks;
		const int SovietGroupSize = 5;

		int sovietParadropTicks;
		const int ParadropIncrement = 200;
		static readonly string[] ParadropTerrainTypes = { "Clear", "Road", "Rough", "Beach", "Ore" };
		static readonly string[] SovietParadroppers = { "e1", "e1", "e3", "e3", "e4" };
		int sovietParadrops;
		int maxSovietYaks;

		int attackAtFrame;
		int attackAtFrameIncrement;
		int minAttackAtFrame;

		Actor allies1EntryPoint;
		Actor allies1MovePoint;

		Actor allies2EntryPoint;
		Actor allies2MovePoint;

		const string McvName = "mcv";
		const string YakName = "yak";

		string difficulty;

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
			{
				SpawnAlliedUnit(McvName);
				evacuateWidget = new InfoWidget("");
				Ui.Root.AddChild(evacuateWidget);
				UpdateUnitsEvacuated();
			}
			if (world.FrameNumber == attackAtFrame)
			{
				SpawnSovietUnits();
				attackAtFrame += attackAtFrameIncrement;
				attackAtFrameIncrement = Math.Max(attackAtFrameIncrement - 5, minAttackAtFrame);
			}

			if (world.FrameNumber == ReinforcementsTicks1 || world.FrameNumber == ReinforcementsTicks2)
				Sound.Play("reinfor1.aud");

			if (world.FrameNumber % 25 == 0)
			{
				if (world.FrameNumber >= ReinforcementsTicks1 && currentReinforcement1 < Reinforcements1.Length)
					SpawnAlliedUnit(Reinforcements1[currentReinforcement1++]);

				if (world.FrameNumber >= ReinforcementsTicks2 && currentReinforcement2 < Reinforcements2.Length)
					SpawnAlliedUnit(Reinforcements2[currentReinforcement2++]);
			}


			if (sovietParadrops > 0)
			{
				if (world.FrameNumber == sovietParadropTicks)
					Sound.Play("sovfapp1.aud");

				if (world.FrameNumber >= sovietParadropTicks && world.FrameNumber % ParadropIncrement == 0)
				{
					CPos lz;
					CPos entry;
					do
					{
						var x = world.SharedRandom.Next(paradropBox.X, paradropBox.X + paradropBox.Width);
						var y = world.SharedRandom.Next(paradropBox.Y, paradropBox.Y + paradropBox.Height);
						entry = new CPos(0, y);
						lz = new CPos(x, y);
					}
					while (!ParadropTerrainTypes.Contains(world.GetTerrainType(lz)));
					MissionUtils.Paradrop(world, soviets, SovietParadroppers, entry, lz);
					sovietParadrops--;
				}
			}
			if (world.FrameNumber % 25 == 0)
				ManageSovietUnits();

			if (destroyAirbases.Status != ObjectiveStatus.Completed)
			{
				if (world.FrameNumber % 25 == 0)
					BuildSovietAircraft();

				ManageSovietAircraft();
			}

			EvacuateAlliedUnits(exit1TopLeft.Location, exit1BottomRight.Location, exit1ExitPoint.Location);
			EvacuateAlliedUnits(exit2TopLeft.Location, exit2BottomRight.Location, exit2ExitPoint.Location);

			CheckSovietAirbases();

			if (!world.Actors.Any(a => (a.Owner == allies1 || a.Owner == allies2) && a.IsInWorld && !a.IsDead()
				&& ((a.HasTrait<Building>() && !a.HasTrait<Wall>()) || a.HasTrait<BaseBuilding>())))
			{
				evacuateUnits.Status = ObjectiveStatus.Failed;
				OnObjectivesUpdated(true);
				MissionFailed("The remaining Allied forces in the area have been wiped out.");
			}
		}

		Actor FirstUnshroudedOrDefault(IEnumerable<Actor> actors, World world, int shroudRange)
		{
			return actors.FirstOrDefault(u => world.FindAliveCombatantActorsInCircle(u.CenterPosition, WDist.FromCells(shroudRange)).All(a => !a.HasTrait<CreatesShroud>()));
		}

		void ManageSovietAircraft()
		{
			var enemies = world.Actors
				.Where(u => (u.Owner == allies1 || u.Owner == allies2)
				&& ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || u.HasTrait<Mobile>()) && u.IsInWorld && !u.IsDead()
				&& (!u.HasTrait<Spy>() || !u.Trait<Spy>().Disguised || (u.Trait<Spy>().Disguised && u.Trait<Spy>().disguisedAsPlayer != soviets)));

			foreach (var aircraft in SovietAircraft())
			{
				var pos = aircraft.CenterPosition;
				var plane = aircraft.Trait<Plane>();
				var ammo = aircraft.Trait<LimitedAmmo>();
				if ((pos.Z == 0 && ammo.FullAmmo()) || (pos.Z != 0 && ammo.HasAmmo()))
				{
					var enemy = FirstUnshroudedOrDefault(enemies.OrderBy(u => (aircraft.CenterPosition - u.CenterPosition).LengthSquared), world, 10);
					if (enemy != null)
					{
						if (!aircraft.IsIdle && aircraft.GetCurrentActivity().GetType() != typeof(FlyAttack))
							aircraft.CancelActivity();

						if (pos.Z == 0)
							plane.UnReserve();

						aircraft.QueueActivity(new FlyAttack(Target.FromActor(enemy)));
					}
				}
				else if (pos.Z != 0 && !LandIsQueued(aircraft))
				{
					aircraft.CancelActivity();
					aircraft.QueueActivity(new ReturnToBase(aircraft, null));
					aircraft.QueueActivity(new ResupplyAircraft());
				}
			}
		}

		bool LandIsQueued(Actor actor)
		{
			for (var a = actor.GetCurrentActivity(); a != null; a = a.NextActivity)
				if (a is ReturnToBase || a is Land) return true;
			return false;
		}

		void BuildSovietAircraft()
		{
			var queue = MissionUtils.FindQueues(world, soviets, "Plane").FirstOrDefault(q => q.CurrentItem() == null);
			if (queue == null || SovietAircraft().Count() >= maxSovietYaks) return;

			queue.ResolveOrder(queue.self, Order.StartProduction(queue.self, YakName, 1));
		}

		IEnumerable<Actor> SovietAircraft()
		{
			return world.Actors.Where(a => a.HasTrait<AttackPlane>() && a.Owner == soviets && a.IsInWorld && !a.IsDead());
		}

		void CheckSovietAirbases()
		{
			if (destroyAirbases.Status != ObjectiveStatus.Completed && sovietAirfields.All(a => a.IsDead() || a.Owner != soviets))
			{
				destroyAirbases.Status = ObjectiveStatus.Completed;
				OnObjectivesUpdated(true);
			}
		}

		void SpawnSovietUnits()
		{
			var route = world.SharedRandom.Next(sovietEntryPoints.Length);
			var spawnPoint = sovietEntryPoints[route];
			var rallyPoint = sovietRallyPoints[route];

			IEnumerable<string> units;
			if (world.FrameNumber >= sovietUnits2Ticks)
				units = SovietUnits2;
			else
				units = SovietUnits1;

			var unit = world.CreateActor(units.Random(world.SharedRandom),
				new TypeDictionary
				{
					new LocationInit(spawnPoint),
					new OwnerInit(soviets)
				});
			unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, new Move.Move(rallyPoint, 3)));
		}

		void AttackNearestAlliedActor(Actor self)
		{
			var enemies = world.Actors.Where(u => u.AppearsHostileTo(self) && (u.Owner == allies1 || u.Owner == allies2)
					&& ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || u.HasTrait<Mobile>()) && u.IsInWorld && !u.IsDead());

			var enemy = FirstUnshroudedOrDefault(enemies.OrderBy(u => (self.CenterPosition - u.CenterPosition).LengthSquared), world, 10);
			if (enemy != null)
				self.QueueActivity(new AttackMove.AttackMoveActivity(self, new Attack(Target.FromActor(enemy), WDist.FromCells(3))));
		}

		void ManageSovietUnits()
		{
			foreach (var rallyPoint in sovietRallyPoints)
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
				.Except(sovietRallyPoints.SelectMany(rp => world.FindAliveCombatantActorsInCircle(rp.CenterPosition, WDist.FromCells(10))));

			foreach (var unit in scatteredUnits)
				AttackNearestAlliedActor(unit);
		}

		void SpawnAlliedUnit(string actor)
		{
			SpawnAndMove(actor, allies1, allies1EntryPoint.Location, allies1MovePoint.Location);
			if (allies1 != allies2)
				SpawnAndMove(actor, allies2, allies2EntryPoint.Location, allies2MovePoint.Location);
		}

		Actor SpawnAndMove(string actor, Player owner, CPos entry, CPos to)
		{
			var unit = world.CreateActor(actor, new TypeDictionary
			{
				new OwnerInit(owner),
				new LocationInit(entry),
				new FacingInit(Traits.Util.GetFacing(to - entry, 0))
			});
			unit.QueueActivity(new Move.Move(to));
			return unit;
		}

		void UpdateUnitsEvacuated()
		{
			evacuateWidget.Text = ShortEvacuateTemplate.F(unitsEvacuated, unitsEvacuatedThreshold);
			if (evacuateUnits.Status == ObjectiveStatus.InProgress && unitsEvacuated >= unitsEvacuatedThreshold)
			{
				evacuateUnits.Status = ObjectiveStatus.Completed;
				OnObjectivesUpdated(true);
				MissionAccomplished("The remaining Allied forces in the area have evacuated.");
			}
		}

		void EvacuateAlliedUnits(CPos tl, CPos br, CPos exit)
		{
			var units = world.FindAliveCombatantActorsInBox(tl, br)
				.Where(u => u.HasTrait<Mobile>() && !u.HasTrait<Aircraft>() && (u.Owner == allies1 || u.Owner == allies2));

			foreach (var unit in units)
			{
				unit.CancelActivity();
				unit.ChangeOwner(allies);
				unitsEvacuated++;

				var createsShroud = unit.TraitOrDefault<CreatesShroud>();
				if (createsShroud != null && evacuateMgg.Status == ObjectiveStatus.InProgress)
				{
					evacuateMgg.Status = ObjectiveStatus.Completed;
					OnObjectivesUpdated(true);
				}

				var cargo = unit.TraitOrDefault<Cargo>();
				if (cargo != null)
					unitsEvacuated += cargo.Passengers.Count();

				UpdateUnitsEvacuated();
				unit.QueueActivity(new Move.Move(exit));
				unit.QueueActivity(new RemoveSelf());
			}
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;

			difficulty = w.LobbyInfo.GlobalSettings.Difficulty;
			Game.Debug("{0} difficulty selected".F(difficulty));

			allies1 = w.Players.Single(p => p.InternalName == "Allies1");
			allies2 = w.Players.SingleOrDefault(p => p.InternalName == "Allies2");

			var res = allies1.PlayerActor.Trait<PlayerResources>();
			if (allies2 == null)
			{
				res.Cash = 10000;
				allies2 = allies1;
			}
			else
			{
				res.Cash = 5000;
				res = allies2.PlayerActor.Trait<PlayerResources>();
				res.Cash = 5000;
			}

			attackAtFrame = attackAtFrameIncrement = difficulty == "Hard" || difficulty == "Normal" ? 500 : 600;
			minAttackAtFrame = difficulty == "Hard" || difficulty == "Normal" ? 100 : 150;
			unitsEvacuatedThreshold = difficulty == "Hard" ? 200 : difficulty == "Normal" ? 100 : 50;
			maxSovietYaks = difficulty == "Hard" ? 4 : difficulty == "Normal" ? 2 : 0;
			sovietParadrops = difficulty == "Hard" ? 40 : difficulty == "Normal" ? 20 : 0;
			sovietParadropTicks = difficulty == "Hard" ? 1500 * 17 : 1500 * 20;
			sovietUnits2Ticks = difficulty == "Hard" ? 1500 * 12 : 1500 * 15;

			evacuateUnits.Text = evacuateUnits.Text.F(unitsEvacuatedThreshold);

			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");

			var actors = w.WorldActor.Trait<SpawnMapActors>().Actors;
			exit1TopLeft = actors["Exit1TopLeft"];
			exit1BottomRight = actors["Exit1BottomRight"];
			exit1ExitPoint = actors["Exit1ExitPoint"];
			exit2TopLeft = actors["Exit2TopLeft"];
			exit2BottomRight = actors["Exit2BottomRight"];
			exit2ExitPoint = actors["Exit2ExitPoint"];
			allies1EntryPoint = actors["Allies1EntryPoint"];
			allies1MovePoint = actors["Allies1MovePoint"];
			allies2EntryPoint = actors["Allies2EntryPoint"];
			allies2MovePoint = actors["Allies2MovePoint"];
			sovietEntryPoint1 = actors["SovietEntryPoint1"];
			sovietEntryPoint2 = actors["SovietEntryPoint2"];
			sovietEntryPoint3 = actors["SovietEntryPoint3"];
			sovietEntryPoint4 = actors["SovietEntryPoint4"];
			sovietEntryPoint5 = actors["SovietEntryPoint5"];
			sovietEntryPoint6 = actors["SovietEntryPoint6"];
			sovietEntryPoints = new[] { sovietEntryPoint1, sovietEntryPoint2, sovietEntryPoint3, sovietEntryPoint4, sovietEntryPoint5, sovietEntryPoint6 }.Select(p => p.Location).ToArray();
			sovietRallyPoint1 = actors["SovietRallyPoint1"];
			sovietRallyPoint2 = actors["SovietRallyPoint2"];
			sovietRallyPoint3 = actors["SovietRallyPoint3"];
			sovietRallyPoint4 = actors["SovietRallyPoint4"];
			sovietRallyPoint5 = actors["SovietRallyPoint5"];
			sovietRallyPoint6 = actors["SovietRallyPoint6"];
			sovietRallyPoints = new[] { sovietRallyPoint1, sovietRallyPoint2, sovietRallyPoint3, sovietRallyPoint4, sovietRallyPoint5, sovietRallyPoint6 }.Select(p => p.Location).ToArray();
			sovietAirfields = actors.Values.Where(a => a.Owner == soviets && a.HasTrait<Production>() && a.Info.Traits.Get<ProductionInfo>().Produces.Contains("Plane")).ToArray();
			var topLeft = actors["ParadropBoxTopLeft"];
			var bottomRight = actors["ParadropBoxBottomRight"];
			paradropBox = new Rectangle(topLeft.Location.X, topLeft.Location.Y, bottomRight.Location.X - topLeft.Location.X, bottomRight.Location.Y - topLeft.Location.Y);

			if (w.LocalPlayer == null || w.LocalPlayer == allies1)
				wr.Viewport.Center(allies1EntryPoint.CenterPosition);
			else
				wr.Viewport.Center(allies2EntryPoint.CenterPosition);

			OnObjectivesUpdated(false);
			MissionUtils.PlayMissionMusic();
		}
	}
}
