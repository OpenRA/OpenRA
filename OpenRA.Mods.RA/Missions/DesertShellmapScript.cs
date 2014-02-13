#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	class DesertShellmapScriptInfo : TraitInfo<DesertShellmapScript>, Requires<SpawnMapActorsInfo> { }

	class DesertShellmapScript : ITick, IWorldLoaded
	{
		World world;
		WorldRenderer worldRenderer;
		Player allies;
		Player soviets;
		Player neutral;

		WPos[] viewportTargets;
		WPos viewportTarget;
		int viewportTargetNumber;
		WPos viewportOrigin;
		int mul;
		int div = 400;
		int waitTicks = 0;

		int nextCivilianMove = 1;

		Actor attackLocation;
		Actor coastWP1;
		Actor coastWP2;
		int coastUnitsLeft;
		static readonly string[] CoastUnits = { "e1", "e1", "e2", "e3", "e4" };

		Actor paradrop1LZ;
		Actor paradrop1Entry;
		Actor paradrop2LZ;
		Actor paradrop2Entry;
		static readonly string[] ParadropUnits = { "e1", "e1", "e1", "e2", "e2" };

		Actor offmapAttackerSpawn1;
		Actor offmapAttackerSpawn2;
		Actor offmapAttackerSpawn3;
		Actor[] offmapAttackerSpawns;
		static readonly string[] OffmapAttackers = { "ftrk", "apc", "ttnk", "1tnk" };
		static readonly string[] AttackerCargo = { "e1", "e2", "e3", "e4" };

		static readonly string[] HeavyTanks = { "3tnk", "3tnk", "3tnk", "3tnk", "3tnk", "3tnk" };
		Actor heavyTankSpawn;
		Actor heavyTankWP;
		static readonly string[] MediumTanks = { "2tnk", "2tnk", "2tnk", "2tnk", "2tnk", "2tnk" };
		Actor mediumTankChronoSpawn;

		static readonly string[] ChinookCargo = { "e1", "e1", "e1", "e1", "e3", "e3" };

		static readonly string[] InfantryProductionUnits = { "e1", "e3" };
		static readonly string[] VehicleProductionUnits = { "jeep", "1tnk", "2tnk", "arty" };
		Actor alliedBarracks;
		Actor alliedWarFactory;

		Dictionary<string, Actor> actors;

		Actor chronosphere;
		Actor ironCurtain;

		CPos[] mig1Waypoints;
		CPos[] mig2Waypoints;

		Actor chinook1Entry;
		Actor chinook1LZ;
		Actor chinook2Entry;
		Actor chinook2LZ;

		public void Tick(Actor self)
		{
			if (world.FrameNumber % 100 == 0)
			{
				var actor = OffmapAttackers.Random(world.SharedRandom);
				var spawn = offmapAttackerSpawns.Random(world.SharedRandom);
				var u = world.CreateActor(actor, soviets, spawn.Location, Traits.Util.GetFacing(attackLocation.Location - spawn.Location, 0));
				var cargo = u.TraitOrDefault<Cargo>();
				if (cargo != null)
				{
					while (cargo.HasSpace(1))
						cargo.Load(u, world.CreateActor(false, AttackerCargo.Random(world.SharedRandom), soviets, null, null));
				}
				u.QueueActivity(new AttackMove.AttackMoveActivity(u, new Move.Move(attackLocation.Location, 0)));
			}

			if (world.FrameNumber % 25 == 0)
			{
				foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.IsIdle && !a.IsDead()
					&& a.HasTrait<AttackBase>() && a.HasTrait<Mobile>()).Except(actors.Values))
					MissionUtils.AttackNearestLandActor(true, actor, actor.Owner == soviets ? allies : soviets);

				MissionUtils.StartProduction(world, allies, "Infantry", InfantryProductionUnits.Random(world.SharedRandom));
				MissionUtils.StartProduction(world, allies, "Vehicle", VehicleProductionUnits.Random(world.SharedRandom));
			}

			if (world.FrameNumber % 20 == 0 && coastUnitsLeft-- > 0)
			{
				var u = world.CreateActor(CoastUnits.Random(world.SharedRandom), soviets, coastWP1.Location, null);
				u.QueueActivity(new Move.Move(coastWP2.Location, 0));
				u.QueueActivity(new AttackMove.AttackMoveActivity(u, new Move.Move(attackLocation.Location, 0)));
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

			if (world.FrameNumber == 1)
			{
				MissionUtils.Paradrop(world, soviets, ParadropUnits, paradrop1Entry.Location, paradrop1LZ.Location);
				MissionUtils.Paradrop(world, soviets, ParadropUnits, paradrop2Entry.Location, paradrop2LZ.Location);
			}

			if (--waitTicks <= 0)
			{
				if (++mul <= div)
					worldRenderer.Viewport.Center(WPos.Lerp(viewportOrigin, viewportTarget, mul, div));
				else
				{
					mul = 0;
					viewportOrigin = viewportTarget;
					viewportTarget = viewportTargets[(viewportTargetNumber = (viewportTargetNumber + 1) % viewportTargets.Length)];
					waitTicks = 100;

					if (viewportTargetNumber == 0)
					{
						coastUnitsLeft = 15;
						SendChinookReinforcements(chinook1Entry.Location, chinook1LZ);
						SendChinookReinforcements(chinook2Entry.Location, chinook2LZ);
					}
					if (viewportTargetNumber == 1)
					{
						MissionUtils.Paradrop(world, soviets, ParadropUnits, paradrop1Entry.Location, paradrop1LZ.Location);
						MissionUtils.Paradrop(world, soviets, ParadropUnits, paradrop2Entry.Location, paradrop2LZ.Location);
					}
					if (viewportTargetNumber == 2)
					{
						AttackWithHeavyTanks();
						ChronoSpawnMediumTanks();
					}
					if (viewportTargetNumber == 4)
					{
						FlyMigs(mig1Waypoints);
						FlyMigs(mig2Waypoints);
					}
				}
			}

			MissionUtils.CapOre(soviets);
		}

		void AttackWithHeavyTanks()
		{
			foreach (var tank in HeavyTanks)
			{
				var u = world.CreateActor(tank, soviets, heavyTankSpawn.Location, Traits.Util.GetFacing(heavyTankWP.Location - heavyTankSpawn.Location, 0));
				u.QueueActivity(new AttackMove.AttackMoveActivity(u, new Move.Move(heavyTankWP.Location, 0)));
			}
			ironCurtain.Trait<IronCurtainPower>().Activate(ironCurtain, new Order { TargetLocation = heavyTankSpawn.Location });
		}

		void ChronoSpawnMediumTanks()
		{
			var chronoInfo = new List<Pair<Actor, CPos>>();
			foreach (var tank in MediumTanks.Select((x, i) => new { x, i }))
			{
				var u = world.CreateActor(tank.x, allies, mediumTankChronoSpawn.Location, Traits.Util.GetFacing(heavyTankWP.Location - mediumTankChronoSpawn.Location, 0));
				chronoInfo.Add(Pair.New(u, new CPos(mediumTankChronoSpawn.Location.X + tank.i, mediumTankChronoSpawn.Location.Y)));
			}
			RASpecialPowers.Chronoshift(world, chronoInfo, chronosphere, -1, false);
			foreach (var tank in chronoInfo)
				tank.First.QueueActivity(new AttackMove.AttackMoveActivity(tank.First, new Move.Move(heavyTankSpawn.Location, 0)));
		}

		void FlyMigs(CPos[] waypoints)
		{
			var m = world.CreateActor("mig", new TypeDictionary
			{
				new OwnerInit(soviets),
				new LocationInit(waypoints[0]),
				new FacingInit(Traits.Util.GetFacing(waypoints[1] - waypoints[0], 0))
			});
			foreach (var waypoint in waypoints)
				m.QueueActivity(new Fly(m, Target.FromCell(waypoint)));
			m.QueueActivity(new RemoveSelf());
		}

		void SendChinookReinforcements(CPos entry, Actor lz)
		{
			var chinook = world.CreateActor("tran", allies, entry, Traits.Util.GetFacing(lz.Location - entry, 0));
			var cargo = chinook.Trait<Cargo>();

			while (cargo.HasSpace(1))
				cargo.Load(chinook, world.CreateActor(false, ChinookCargo.Random(world.SharedRandom), allies, null, null));

			var exit = lz.Info.Traits.WithInterface<ExitInfo>().FirstOrDefault();
			var offset = (exit != null) ? exit.SpawnOffset : WVec.Zero;

			chinook.QueueActivity(new HeliFly(chinook, Target.FromPos(lz.CenterPosition + offset))); // no reservation of hpad but it's not needed
			chinook.QueueActivity(new Turn(0));
			chinook.QueueActivity(new HeliLand(false));
			chinook.QueueActivity(new UnloadCargo(chinook, true));
			chinook.QueueActivity(new Wait(150));
			chinook.QueueActivity(new HeliFly(chinook, Target.FromCell(entry)));
			chinook.QueueActivity(new RemoveSelf());
		}

		void InitializeAlliedFactories()
		{
			alliedBarracks.Trait<PrimaryBuilding>().SetPrimaryProducer(alliedBarracks, true);
			alliedWarFactory.Trait<PrimaryBuilding>().SetPrimaryProducer(alliedWarFactory, true);
		}

		public void WorldLoaded(World w, WorldRenderer wr)
		{
			world = w;
			worldRenderer = wr;
			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			neutral = w.Players.Single(p => p.InternalName == "Neutral");

			actors = w.WorldActor.Trait<SpawnMapActors>().Actors;

			attackLocation = actors["AttackLocation"];
			coastWP1 = actors["CoastWP1"];
			coastWP2 = actors["CoastWP2"];

			paradrop1LZ = actors["Paradrop1LZ"];
			paradrop1Entry = actors["Paradrop1Entry"];
			paradrop2LZ = actors["Paradrop2LZ"];
			paradrop2Entry = actors["Paradrop2Entry"];

			var t1 = actors["ViewportTarget1"].CenterPosition;
			var t2 = actors["ViewportTarget2"].CenterPosition;
			var t3 = actors["ViewportTarget3"].CenterPosition;
			var t4 = actors["ViewportTarget4"].CenterPosition;
			var t5 = actors["ViewportTarget5"].CenterPosition;
			viewportTargets = new[] { t1, t2, t3, t4, t5 };

			offmapAttackerSpawn1 = actors["OffmapAttackerSpawn1"];
			offmapAttackerSpawn2 = actors["OffmapAttackerSpawn2"];
			offmapAttackerSpawn3 = actors["OffmapAttackerSpawn3"];
			offmapAttackerSpawns = new[] { offmapAttackerSpawn1, offmapAttackerSpawn2, offmapAttackerSpawn3 };

			heavyTankSpawn = actors["HeavyTankSpawn"];
			heavyTankWP = actors["HeavyTankWP"];
			mediumTankChronoSpawn = actors["MediumTankChronoSpawn"];

			chronosphere = actors["Chronosphere"];
			ironCurtain = actors["IronCurtain"];

			mig1Waypoints = new[] { actors["Mig11"], actors["Mig12"], actors["Mig13"], actors["Mig14"] }.Select(a => a.Location).ToArray();
			mig2Waypoints = new[] { actors["Mig21"], actors["Mig22"], actors["Mig23"], actors["Mig24"] }.Select(a => a.Location).ToArray();

			chinook1Entry = actors["Chinook1Entry"];
			chinook2Entry = actors["Chinook2Entry"];
			chinook1LZ = actors["Chinook1LZ"];
			chinook2LZ = actors["Chinook2LZ"];

			alliedBarracks = actors["AlliedBarracks"];
			alliedWarFactory = actors["AlliedWarFactory"];

			InitializeAlliedFactories();
			
			foreach (var actor in actors.Values)
			{
				if (actor.Owner == allies && actor.HasTrait<AutoTarget>())
					actor.Trait<AutoTarget>().Stance = UnitStance.Defend;
				
				if (actor.IsInWorld && (actor.HasTrait<Bridge>() || actor.Owner == allies || (actor.Owner == soviets && actor.HasTrait<Building>())))
					actor.AddTrait(new Invulnerable());
			}

			viewportOrigin = viewportTargets[0];
			viewportTargetNumber = 1;
			viewportTarget = viewportTargets[1];

			wr.Viewport.Center(viewportOrigin);
			Sound.SoundVolumeModifier = 0.1f;
		}
	}

	class DesertShellmapAutoUnloadInfo : TraitInfo<DesertShellmapAutoUnload>, Requires<CargoInfo> { }

	class DesertShellmapAutoUnload : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			var cargo = self.Trait<Cargo>();
			if (!cargo.IsEmpty(self) && !(self.GetCurrentActivity() is UnloadCargo))
				self.QueueActivity(false, new UnloadCargo(self, true));
		}
	}
}
