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
			{ EvacuateID, new Objective(ObjectiveType.Primary, "Following the rescue of Einstein, the Allies are now being flanked from both sides. Evacuate {0} units before the remaining Allied forces in the area are wiped out.".F(UnitsEvacuatedThreshold), ObjectiveStatus.InProgress) },
			{ AirbaseID, new Objective(ObjectiveType.Secondary, "Destroy the nearby Soviet airbase.", ObjectiveStatus.InProgress) }
		};

		const int EvacuateID = 0;
		const int AirbaseID = 1;

		const int UnitsEvacuatedThreshold = 50;
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

		Actor sovietAirfield1;
		Actor sovietAirfield2;
		Actor sovietAirfield3;
		Actor sovietAirfield4;

		static readonly string[] SovietVehicles = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "v2rl", "v2rl", "ftrk", "ftrk", "apc", "apc", "apc" };
		const int SovietAttackGroupSize = 3;
		const int MaxNumberYaks = 4;

		int attackAtFrame;
		int attackAtFrameIncrement;

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
				SpawnAlliedMcvs();
				evacuateWidget = new InfoWidget("", new float2(Game.viewport.Width * 0.35f, Game.viewport.Height * 0.9f));
				Ui.Root.AddChild(evacuateWidget);
				UpdateUnitsEvacuated();
			}
			if (world.FrameNumber == attackAtFrame)
			{
				SpawnSovietUnits();
				attackAtFrame += attackAtFrameIncrement;
				attackAtFrameIncrement = Math.Max(attackAtFrameIncrement - 5, 250);
			}
			if (objectives[AirbaseID].Status != ObjectiveStatus.Completed)
			{
				BuildSovietAircraft();
				ManageSovietAircraft();
			}
			ManageSovietUnits();
			EvacuateAlliedUnits(exit1TopLeft.CenterLocation, exit1BottomRight.CenterLocation, exit1ExitPoint.Location);
			EvacuateAlliedUnits(exit2TopLeft.CenterLocation, exit2BottomRight.CenterLocation, exit2ExitPoint.Location);
			CheckSovietAirbase();
			if (!world.Actors.Any(a => (a.Owner == allies1 || a.Owner == allies2) && a.IsInWorld && !a.IsDead() && ((a.HasTrait<Building>() && !a.HasTrait<Wall>()) || a.HasTrait<BaseBuilding>())))
			{
				MissionFailed("The remaining Allied forces in the area have been wiped out.");
			}
		}

		void ManageSovietAircraft()
		{
			var enemies = world.Actors.Where(u => (u.Owner == allies1 || u.Owner == allies2) && ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || u.HasTrait<Mobile>()) && u.IsInWorld && !u.IsDead());
			foreach (var aircraft in SovietAircraft())
			{
				var plane = aircraft.Trait<Plane>();
				var ammo = aircraft.Trait<LimitedAmmo>();
				if ((plane.Altitude == 0 && ammo.FullAmmo()) || (plane.Altitude != 0 && ammo.HasAmmo()))
				{
					var enemy = enemies.OrderBy(u => (aircraft.CenterLocation - u.CenterLocation).LengthSquared).FirstUnshroudedOrDefault(world, 10);
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
			for (;;)
			{
				if (a == null) { return false; }
				if (a.GetType() == typeof(ReturnToBase) || a.GetType() == typeof(Land)) { return true; }
				a = a.NextActivity;
			}
		}

		void BuildSovietAircraft()
		{
			var queue = MissionUtils.FindQueues(world, soviets, "Plane").FirstOrDefault(q => q.CurrentItem() == null);
			if (queue == null || SovietAircraft().Count() >= MaxNumberYaks)
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
			if (objectives[AirbaseID].Status != ObjectiveStatus.Completed
				&& (sovietAirfield1.Destroyed || sovietAirfield1.Owner != soviets)
				&& (sovietAirfield2.Destroyed || sovietAirfield2.Owner != soviets)
				&& (sovietAirfield3.Destroyed || sovietAirfield3.Owner != soviets)
				&& (sovietAirfield4.Destroyed || sovietAirfield4.Owner != soviets))
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
			var unit = world.CreateActor(SovietVehicles.Random(world.SharedRandom),
				new TypeDictionary { new LocationInit(spawnPoint), new OwnerInit(soviets) });
			unit.QueueActivity(new AttackMove.AttackMoveActivity(unit, new Move.Move(rallyPoint, 3)));
		}

		void AttackNearestAlliedActor(Actor self)
		{
			var enemies = world.Actors.Where(u => u.IsInWorld && !u.IsDead() && (u.Owner == allies1 || u.Owner == allies2)
				&& ((u.HasTrait<Building>() && !u.HasTrait<Wall>()) || u.HasTrait<Mobile>()));
			var enemy = enemies.OrderBy(u => (self.CenterLocation - u.CenterLocation).LengthSquared).FirstUnshroudedOrDefault(world, 10);
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
				if (units.Count() >= SovietAttackGroupSize)
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

		void SpawnAlliedMcvs()
		{
			var unit = world.CreateActor(McvName, new TypeDictionary
			{
				new LocationInit(allies1EntryPoint.Location), 
				new OwnerInit(allies1),
				new FacingInit(Util.GetFacing(allies1MovePoint.Location - allies1EntryPoint.Location, 0)) 
			});
			unit.QueueActivity(new Move.Move(allies1MovePoint.Location));
			if (allies2 != allies1)
			{
				unit = world.CreateActor(McvName, new TypeDictionary 
				{ 
					new LocationInit(allies2EntryPoint.Location), 
					new OwnerInit(allies2),
					new FacingInit(Util.GetFacing(allies2MovePoint.Location - allies2EntryPoint.Location, 0))
				});
				unit.QueueActivity(new Move.Move(allies2MovePoint.Location));
			}
		}

		void UpdateUnitsEvacuated()
		{
			evacuateWidget.Text = ShortEvacuateTemplate.F(unitsEvacuated, UnitsEvacuatedThreshold);
			if (objectives[EvacuateID].Status == ObjectiveStatus.InProgress && unitsEvacuated >= UnitsEvacuatedThreshold)
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
				unit.QueueActivity(new Move.Move(exit));
				unit.QueueActivity(new CallFunc(() =>
				{
					if (unit.IsDead())
					{
						return;
					}
					unitsEvacuated++;
					var cargo = unit.TraitOrDefault<Cargo>();
					if (cargo != null)
					{
						unitsEvacuated += cargo.Passengers.Count();
					}
					UpdateUnitsEvacuated();
				}));
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
			}
			else
			{
				allies2 = allies1;
				attackAtFrame = 600;
				attackAtFrameIncrement = 600;
			}
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
			sovietAirfield1 = actors["SovietAirfield1"];
			sovietAirfield2 = actors["SovietAirfield2"];
			sovietAirfield3 = actors["SovietAirfield3"];
			sovietAirfield4 = actors["SovietAirfield4"];
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
