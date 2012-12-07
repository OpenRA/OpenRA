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
using OpenRA.Mods.RA.Move;
using OpenRA.Network;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.RA.Missions
{
	class Allies03ScriptInfo : TraitInfo<Allies03Script>, Requires<SpawnMapActorsInfo> { }

	class Allies03Script : IHasObjectives, IWorldLoaded, ITick
	{
		public event ObjectivesUpdatedEventHandler OnObjectivesUpdated = notify => { };

		public IEnumerable<Objective> Objectives { get { return objectives.Values; } }

		Dictionary<int, Objective> objectives = new Dictionary<int, Objective>
		{
			{ EvacuateID, new Objective(ObjectiveType.Primary, "Following the rescue of Einstein, the Allies are now being flanked from both sides. Evacuate {0} units before the remaining Allied forces in the area are wiped out.", ObjectiveStatus.InProgress) },
			{ AirbaseID, new Objective(ObjectiveType.Secondary, "Destroy the nearby Soviet airbases.", ObjectiveStatus.InProgress) }
		};

		const int EvacuateID = 0;
		const int AirbaseID = 1;

		int unitsEvacuatedThreshold;
		int unitsEvacuated;
		InfoWidget evacuateWidget;
		const string ShortEvacuateTemplate = "{0}/{1} units evacuated";

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

		static readonly string[] SovietVehicles1 = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "ftrk", "ftrk", "apc", "apc", "apc" };
		static readonly string[] SovietVehicles2 = { "4tnk", "4tnk", "4tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "ftrk", "apc" };
		const int SovietVehicles2Ticks = 1500 * 15;
		const int SovietGroupSize = 5;

		const int ParadropTicks = 1500 * 20;
		const int ParadropIncrement = 200;
		static readonly string[] ParadropTerrainTypes = { "Clear", "Road", "Rough", "Beach", "Ore" };
		static readonly string[] SovietParadroppers = { "e1", "e1", "e3", "e3", "e4" };
		int paradrops = 20;
		const int maxSovietYaks = 2;

		int attackAtFrame;
		int attackAtFrameIncrement;
		int minAttackAtFrame;

		Actor allies1EntryPoint;
		Actor allies1MovePoint;

		Actor allies2EntryPoint;
		Actor allies2MovePoint;

		const string McvName = "mcv";
		const string YakName = "yak";

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
				SpawnAlliedUnit(McvName);
				evacuateWidget = new InfoWidget("", new float2(Game.viewport.Width * 0.35f, Game.viewport.Height * 0.9f));
				Ui.Root.AddChild(evacuateWidget);
				UpdateUnitsEvacuated();
			}
			if (world.FrameNumber == attackAtFrame)
			{
				SpawnSovietUnits();
				attackAtFrame += attackAtFrameIncrement;
				attackAtFrameIncrement = Math.Max(attackAtFrameIncrement - 5, minAttackAtFrame);
			}
			if (world.FrameNumber >= ReinforcementsTicks1 && currentReinforcement1 < Reinforcements1.Length)
			{
				if (world.FrameNumber == ReinforcementsTicks1) { Sound.Play("reinfor1.aud"); }
				if (world.FrameNumber % 25 == 0) { SpawnAlliedUnit(Reinforcements1[currentReinforcement1++]); }
			}
			if (world.FrameNumber >= ReinforcementsTicks2 && currentReinforcement2 < Reinforcements2.Length)
			{
				if (world.FrameNumber == ReinforcementsTicks2) { Sound.Play("reinfor1.aud"); }
				if (world.FrameNumber % 25 == 0) { SpawnAlliedUnit(Reinforcements2[currentReinforcement2++]); }
			}
			if (world.FrameNumber == ParadropTicks)
			{
				Sound.Play("sovfapp1.aud");
			}
			if (world.FrameNumber >= ParadropTicks && paradrops > 0 && world.FrameNumber % ParadropIncrement == 0)
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
				paradrops--;
			}
			if (world.FrameNumber % 25 == 0)
			{
				ManageSovietUnits();
			}
			if (objectives[AirbaseID].Status != ObjectiveStatus.Completed)
			{
				if (world.FrameNumber % 25 == 0)
				{
					BuildSovietAircraft();
				}
				ManageSovietAircraft();
			}
			EvacuateAlliedUnits(exit1TopLeft.CenterLocation, exit1BottomRight.CenterLocation, exit1ExitPoint.Location);
			EvacuateAlliedUnits(exit2TopLeft.CenterLocation, exit2BottomRight.CenterLocation, exit2ExitPoint.Location);
			CheckSovietAirbase();
			if (!world.Actors.Any(a => (a.Owner == allies1 || a.Owner == allies2) && a.IsInWorld && !a.IsDead()
				&& ((a.HasTrait<Building>() && !a.HasTrait<Wall>()) || a.HasTrait<BaseBuilding>())))
			{
				MissionFailed("The remaining Allied forces in the area have been wiped out.");
			}
		}

		Actor FirstUnshroudedOrDefault(IEnumerable<Actor> actors, World world, int shroudRange)
		{
			return actors.FirstOrDefault(u => world.FindAliveCombatantActorsInCircle(u.CenterLocation, shroudRange).All(a => !a.HasTrait<CreatesShroud>()));
		}

		void ManageSovietAircraft()
		{
			var enemies = world.Actors
				.Where(u => (u.Owner == allies1 || u.Owner == allies2)
				&& ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || u.HasTrait<Mobile>()) && u.IsInWorld && !u.IsDead()
				&& (!u.HasTrait<Spy>() || !u.Trait<Spy>().Disguised || (u.Trait<Spy>().Disguised && u.Trait<Spy>().disguisedAsPlayer != soviets)));

			foreach (var aircraft in SovietAircraft())
			{
				var plane = aircraft.Trait<Plane>();
				var ammo = aircraft.Trait<LimitedAmmo>();
				if ((plane.Altitude == 0 && ammo.FullAmmo()) || (plane.Altitude != 0 && ammo.HasAmmo()))
				{
					var enemy = FirstUnshroudedOrDefault(enemies.OrderBy(u => (aircraft.CenterLocation - u.CenterLocation).LengthSquared), world, 10);
					if (enemy != null)
					{
						if (!aircraft.IsIdle && aircraft.GetCurrentActivity().GetType() != typeof(FlyAttack))
						{
							aircraft.CancelActivity();
						}
						if (plane.Altitude == 0)
						{
							plane.UnReserve();
						}
						aircraft.QueueActivity(new FlyAttack(Target.FromActor(enemy)));
					}
				}
				else if (plane.Altitude != 0 && !LandIsQueued(aircraft))
				{
					aircraft.CancelActivity();
					aircraft.QueueActivity(new ReturnToBase(aircraft, null));
					aircraft.QueueActivity(new ResupplyAircraft());
				}
			}
		}

		bool LandIsQueued(Actor actor)
		{
			var a = actor.GetCurrentActivity();
			for (; ; )
			{
				if (a == null) { return false; }
				if (a.GetType() == typeof(ReturnToBase) || a.GetType() == typeof(Land)) { return true; }
				a = a.NextActivity;
			}
		}

		void BuildSovietAircraft()
		{
			var queue = MissionUtils.FindQueues(world, soviets, "Plane").FirstOrDefault(q => q.CurrentItem() == null);
			if (queue == null || SovietAircraft().Count() >= maxSovietYaks)
			{
				return;
			}
			if (Game.IsHost)
			{
				world.IssueOrder(Order.StartProduction(queue.self, YakName, 1));
			}
		}

		IEnumerable<Actor> SovietAircraft()
		{
			return world.Actors.Where(a => a.HasTrait<AttackPlane>() && a.Owner == soviets && a.IsInWorld && !a.IsDead());
		}

		void CheckSovietAirbase()
		{
			if (objectives[AirbaseID].Status != ObjectiveStatus.Completed && sovietAirfields.All(a => a.IsDead() || a.Owner != soviets))
			{
				objectives[AirbaseID].Status = ObjectiveStatus.Completed;
				OnObjectivesUpdated(true);
			}
		}

		void SpawnSovietUnits()
		{
			var route = world.SharedRandom.Next(sovietEntryPoints.Length);
			var spawnPoint = sovietEntryPoints[route];
			var rallyPoint = sovietRallyPoints[route];
			IEnumerable<string> units;
			if (world.FrameNumber >= SovietVehicles2Ticks)
			{
				units = SovietVehicles2;
			}
			else
			{
				units = SovietVehicles1;
			}
			var unit = world.CreateActor(units.Random(world.SharedRandom), new TypeDictionary { new LocationInit(spawnPoint), new OwnerInit(soviets) });
			unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, new Move.Move(rallyPoint, 3)));
		}

		void AttackNearestAlliedActor(Actor self)
		{
			var enemies = world.Actors
				.Where(u => (u.Owner == allies1 || u.Owner == allies2)
				&& ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || u.HasTrait<Mobile>()) && u.IsInWorld && !u.IsDead()
				&& (!u.HasTrait<Spy>() || !u.Trait<Spy>().Disguised || (u.Trait<Spy>().Disguised && u.Trait<Spy>().disguisedAsPlayer != soviets)));
			var enemy = FirstUnshroudedOrDefault(enemies.OrderBy(u => (self.CenterLocation - u.CenterLocation).LengthSquared), world, 10);
			if (enemy != null)
			{
				self.QueueActivity(new AttackMove.AttackMoveActivity(self, new Attack(Target.FromActor(enemy), 3)));
			}
		}

		void ManageSovietUnits()
		{
			foreach (var rallyPoint in sovietRallyPoints)
			{
				var units = world.FindAliveCombatantActorsInCircle(Util.CenterOfCell(rallyPoint), 10)
					.Where(u => u.IsIdle && u.HasTrait<Mobile>() && u.Owner == soviets);
				if (units.Count() >= SovietGroupSize)
				{
					foreach (var unit in units)
					{
						AttackNearestAlliedActor(unit);
					}
				}
			}
			var scatteredUnits = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && u.HasTrait<Mobile>() && u.IsIdle && u.Owner == soviets)
				.Except(world.WorldActor.Trait<SpawnMapActors>().Actors.Values)
				.Except(sovietRallyPoints.SelectMany(rp => world.FindAliveCombatantActorsInCircle(Util.CenterOfCell(rp), 10)));
			foreach (var unit in scatteredUnits)
			{
				AttackNearestAlliedActor(unit);
			}
		}

		void SpawnAlliedUnit(string actor)
		{
			var unit = SpawnAndMove(actor, allies1, allies1EntryPoint.Location, allies1MovePoint.Location);
			if (allies2 != allies1)
			{
				unit = SpawnAndMove(actor, allies2, allies2EntryPoint.Location, allies2MovePoint.Location);
			}
		}

		Actor SpawnAndMove(string actor, Player owner, CPos entry, CPos to)
		{
			var unit = world.CreateActor(actor, new TypeDictionary
			{
				new OwnerInit(owner),
				new LocationInit(entry),
				new FacingInit(Util.GetFacing(to - entry, 0))
			});
			unit.QueueActivity(new Move.Move(to));
			return unit;
		}

		void UpdateUnitsEvacuated()
		{
			evacuateWidget.Text = ShortEvacuateTemplate.F(unitsEvacuated, unitsEvacuatedThreshold);
			if (objectives[EvacuateID].Status == ObjectiveStatus.InProgress && unitsEvacuated >= unitsEvacuatedThreshold)
			{
				objectives[EvacuateID].Status = ObjectiveStatus.Completed;
				OnObjectivesUpdated(true);
				MissionAccomplished("The remaining Allied forces in the area have evacuated.");
			}
		}

		void EvacuateAlliedUnits(PPos a, PPos b, CPos exit)
		{
			var units = world.FindAliveCombatantActorsInBox(a, b)
				.Where(u => u.HasTrait<Mobile>() && !u.HasTrait<Aircraft>() && (u.Owner == allies1 || u.Owner == allies2));
			foreach (var unit in units)
			{
				unit.CancelActivity();
				unit.ChangeOwner(allies);
				unitsEvacuated++;
				var cargo = unit.TraitOrDefault<Cargo>();
				if (cargo != null)
				{
					unitsEvacuated += cargo.Passengers.Count();
				}
				UpdateUnitsEvacuated();
				unit.QueueActivity(new Move.Move(exit));
				unit.QueueActivity(new RemoveSelf());
			}
		}

		public void WorldLoaded(World w)
		{
			world = w;
			allies1 = w.Players.Single(p => p.InternalName == "Allies1");
			allies2 = w.Players.SingleOrDefault(p => p.InternalName == "Allies2");
			if (allies2 != null)
			{
				attackAtFrame = 500;
				attackAtFrameIncrement = 500;
				minAttackAtFrame = 200;
				unitsEvacuatedThreshold = 100;
			}
			else
			{
				allies2 = allies1;
				attackAtFrame = 600;
				attackAtFrameIncrement = 600;
				minAttackAtFrame = 100;
				unitsEvacuatedThreshold = 50;
			}
			objectives[EvacuateID].Text = objectives[EvacuateID].Text.F(unitsEvacuatedThreshold);
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
			{
				Game.MoveViewport(allies1EntryPoint.Location.ToFloat2());
			}
			else
			{
				Game.MoveViewport(allies2EntryPoint.Location.ToFloat2());
			}
			PlayMusic();
			OnObjectivesUpdated(false);
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
