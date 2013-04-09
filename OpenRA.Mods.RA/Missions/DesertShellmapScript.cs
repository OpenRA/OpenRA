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
using OpenRA.Mods.RA.Move;
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
		Actor coastRP1;
		Actor coastRP2;
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

		Dictionary<string, Actor> mapActors;

		public void Tick(Actor self)
		{
			if (world.FrameNumber % 100 == 0)
			{
				var u = world.CreateActor(OffmapAttackers.Random(world.SharedRandom), soviets, offmapAttackerSpawns.Random(world.SharedRandom).Location, 128);
				u.QueueActivity(new AttackMove.AttackMoveActivity(u, new Move.Move(attackLocation.Location, 0)));
			}

			if (world.FrameNumber % 25 == 0)
				foreach (var actor in world.Actors.Where(a => a.IsInWorld && a.Owner == soviets && a.IsIdle && !a.IsDead() && a.HasTrait<AttackBase>() && a.HasTrait<Mobile>())
					.Except(mapActors.Values))
					actor.QueueActivity(new AttackMove.AttackMoveActivity(actor, new Move.Move(attackLocation.Location, 0)));

			if (world.FrameNumber % 20 == 0 && coastUnitsLeft-- > 0)
			{
				var u = world.CreateActor(CoastUnits.Random(world.SharedRandom), soviets, coastRP1.Location, null);
				u.QueueActivity(new Move.Move(coastRP2.Location, 0));
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
				}
			}

			MissionUtils.CapOre(soviets);
		}

		public void WorldLoaded(World w)
		{
			world = w;

			allies = w.Players.Single(p => p.InternalName == "Allies");
			soviets = w.Players.Single(p => p.InternalName == "Soviets");
			neutral = w.Players.Single(p => p.InternalName == "Neutral");

			mapActors = w.WorldActor.Trait<SpawnMapActors>().Actors;

			attackLocation = mapActors["AttackLocation"];
			coastRP1 = mapActors["CoastRP1"];
			coastRP2 = mapActors["CoastRP2"];
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

			MissionUtils.Paradrop(world, soviets, ParadropUnits, paradropEntry.Location, paradropLZ.Location);
		}
	}
}
