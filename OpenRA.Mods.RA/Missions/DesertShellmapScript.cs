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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Move;
using OpenRA.Scripting;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Missions
{
	class DesertShellmapScriptInfo : TraitInfo<DesertShellmapScript>, Requires<SpawnMapActorsInfo> { }

	class DesertShellmapScript : ITick, IWorldLoaded
	{
		World world;
		Player allies;
		Player soviets;
		Player neutral;

		List<int2> viewportTargets = new List<int2>();
		int2 viewportTarget;
		int viewportTargetNumber;
		int2 viewportOrigin;
		float mul;
		float div = 400;
		int waitTicks = 0;

		int nextCivilianMove = 1;

		Actor attackLocation;
		Actor coastWP1;
		Actor coastWP2;
		int coastUnitsLeft;
		static readonly string[] CoastUnits = { "e1", "e1", "e2", "e3", "e4" };

		Actor paradropLZ;
		Actor paradropEntry;
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

		Dictionary<string, Actor> mapActors;

		Actor chronosphere;
		Actor ironCurtain;

		CPos[] mig1Waypoints;
		CPos[] mig2Waypoints;

		public void Tick(Actor self)
		{
			if (world.FrameNumber % 100 == 0)
			{
				var actor = OffmapAttackers.Random(world.SharedRandom);
				var spawn = offmapAttackerSpawns.Random(world.SharedRandom);
				var u = world.CreateActor(actor, soviets, spawn.Location, Util.GetFacing(attackLocation.Location - spawn.Location, 0));
				var cargo = u.TraitOrDefault<Cargo>();
				if (cargo != null)
				{
					while (cargo.HasSpace(1))
						cargo.Load(u, world.CreateActor(false, AttackerCargo.Random(world.SharedRandom), soviets, null, null));
				}
				u.QueueActivity(new AttackMove.AttackMoveActivity(u, new Move.Move(attackLocation.Location, 0)));
			}

			if (world.FrameNumber % 25 == 0)
				foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.IsIdle && !a.IsDead()
					&& a.HasTrait<AttackBase>() && a.HasTrait<Mobile>()).Except(mapActors.Values))
						MissionUtils.AttackNearestLandActor(true, actor, actor.Owner == soviets ? allies : soviets);

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
				MissionUtils.Paradrop(world, soviets, ParadropUnits, paradropEntry.Location, paradropLZ.Location);

			if (--waitTicks <= 0)
			{
				if (++mul <= div)
					Game.MoveViewport(float2.Lerp(viewportOrigin, viewportTarget, mul / div));
				else
				{
					mul = 0;
					viewportOrigin = viewportTarget;
					viewportTarget = viewportTargets[(viewportTargetNumber = (viewportTargetNumber + 1) % viewportTargets.Count)];
					waitTicks = 100;

					if (viewportTargetNumber == 0)
						coastUnitsLeft = 15;
					if (viewportTargetNumber == 1)
						MissionUtils.Paradrop(world, soviets, ParadropUnits, paradropEntry.Location, paradropLZ.Location);
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
				var u = world.CreateActor(tank, soviets, heavyTankSpawn.Location, Util.GetFacing(heavyTankWP.Location - heavyTankSpawn.Location, 0));
				u.QueueActivity(new AttackMove.AttackMoveActivity(u, new Move.Move(heavyTankWP.Location, 0)));
			}
			ironCurtain.Trait<IronCurtainPower>().Activate(ironCurtain, new Order { TargetLocation = heavyTankSpawn.Location });
		}

		void ChronoSpawnMediumTanks()
		{
			var chronoInfo = new List<Pair<Actor, CPos>>();
			foreach (var tank in MediumTanks.Select((x, i) => new { x, i }))
			{
				var u = world.CreateActor(tank.x, allies, mediumTankChronoSpawn.Location, Util.GetFacing(heavyTankWP.Location - mediumTankChronoSpawn.Location, 0));
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
				new FacingInit(Util.GetFacing(waypoints[1] - waypoints[0], 0))
			});
			foreach (var waypoint in waypoints)
				m.QueueActivity(Fly.ToCell(waypoint));
			m.QueueActivity(new RemoveSelf());
		}

		public void WorldLoaded(World w)
		{
			world = w;

			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			neutral = w.Players.Single(p => p.InternalName == "Neutral");

			mapActors = w.WorldActor.Trait<SpawnMapActors>().Actors;

			attackLocation = mapActors["AttackLocation"];
			coastWP1 = mapActors["CoastWP1"];
			coastWP2 = mapActors["CoastWP2"];
			paradropLZ = mapActors["ParadropLZ"];
			paradropEntry = mapActors["ParadropEntry"];

			var t1 = mapActors["ViewportTarget1"];
			var t2 = mapActors["ViewportTarget2"];
			var t3 = mapActors["ViewportTarget3"];
			var t4 = mapActors["ViewportTarget4"];
			var t5 = mapActors["ViewportTarget5"];
			viewportTargets = new[] { t1, t2, t3, t4, t5 }.Select(t => t.Location.ToInt2()).ToList();

			offmapAttackerSpawn1 = mapActors["OffmapAttackerSpawn1"];
			offmapAttackerSpawn2 = mapActors["OffmapAttackerSpawn2"];
			offmapAttackerSpawn3 = mapActors["OffmapAttackerSpawn3"];
			offmapAttackerSpawns = new[] { offmapAttackerSpawn1, offmapAttackerSpawn2, offmapAttackerSpawn3 };

			heavyTankSpawn = mapActors["HeavyTankSpawn"];
			heavyTankWP = mapActors["HeavyTankWP"];
			mediumTankChronoSpawn = mapActors["MediumTankChronoSpawn"];

			chronosphere = mapActors["Chronosphere"];
			ironCurtain = mapActors["IronCurtain"];

			mig1Waypoints = new[] { mapActors["Mig11"], mapActors["Mig12"], mapActors["Mig13"], mapActors["Mig14"] }.Select(a => a.Location).ToArray();
			mig2Waypoints = new[] { mapActors["Mig21"], mapActors["Mig22"], mapActors["Mig23"], mapActors["Mig24"] }.Select(a => a.Location).ToArray();

			foreach (var actor in mapActors.Values.Where(a => a.Owner == allies || a.HasTrait<Bridge>()))
			{
				if (actor.Owner == allies && actor.HasTrait<AutoTarget>())
					actor.Trait<AutoTarget>().stance = UnitStance.Defend;
				actor.AddTrait(new Invulnerable());
			}

			viewportOrigin = viewportTargets[0];
			viewportTargetNumber = 1;
			viewportTarget = viewportTargets[1];
			Game.viewport.Center(viewportOrigin);
			Sound.SoundVolumeModifier = 0.25f;
		}
	}

	class DesertShellmapAutoUnloadInfo : TraitInfo<DesertShellmapAutoUnload>, Requires<CargoInfo> { }

	class DesertShellmapAutoUnload : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			var cargo = self.Trait<Cargo>();
			if (!cargo.IsEmpty(self) && !(self.GetCurrentActivity() is UnloadCargo))
				self.QueueActivity(false, new UnloadCargo(true));
		}
	}
}
