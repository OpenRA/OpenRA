#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CrateSpawnerInfo : ITraitInfo
	{
		[Desc("Minimum number of crates")]
		public readonly int Minimum = 1;
		[Desc("Maximum number of crates")]
		public readonly int Maximum = 255;
		[Desc("Average time (seconds) between crate spawn")]
		public readonly int SpawnInterval = 180;
		[Desc("Which terrain types can we drop on?")]
		public readonly string[] ValidGround = { "Clear", "Rough", "Road", "Ore", "Beach" };
		[Desc("Which terrain types count as water?")]
		public readonly string[] ValidWater = { "Water" };
		[Desc("Chance of generating a water crate instead of a land crate")]
		public readonly float WaterChance = .2f;
		[Desc("If a DeliveryAircraft: is specified, then this actor will deliver crates"), ActorReference]
		public readonly string DeliveryAircraft = null;
		[Desc("Crate actors to drop"), ActorReference]
		public readonly string[] CrateActors = { "crate" };
		[Desc("Chance of each crate actor spawning")]
		public readonly int[] CrateActorShares = { 10 };

		public object Create(ActorInitializer init) { return new CrateSpawner(this, init.self); }
	}

	public class CrateSpawner : ITick
	{
		int crates = 0;
		int ticks = 0;
		CrateSpawnerInfo info;
		Actor self;

		public CrateSpawner(CrateSpawnerInfo info, Actor self)
		{
			this.info = info;
			this.self = self;
		}

		public void Tick(Actor self)
		{
			if (!self.World.LobbyInfo.GlobalSettings.Crates)
				return;

			if (--ticks <= 0)
			{
				ticks = info.SpawnInterval * 25;

				var toSpawn = Math.Max(0, info.Minimum - crates)
					+ (crates < info.Maximum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnCrate(self);
			}
		}

		void SpawnCrate(Actor self)
		{
			var threshold = 100;
			var inWater = self.World.SharedRandom.NextFloat() < info.WaterChance;
			var pp = ChooseDropCell(self, inWater, threshold);

			if (pp == null)
				return;

			var p = pp.Value;
			var crateActor = ChooseCrateActor();

			self.World.AddFrameEndTask(w =>
			{
				if (info.DeliveryAircraft != null)
				{
					var crate = w.CreateActor(false, crateActor, new TypeDictionary { new OwnerInit(w.WorldActor.Owner) });
					var startPos = w.ChooseRandomEdgeCell();
					var altitude = self.World.Map.Rules.Actors[info.DeliveryAircraft].Traits.Get<PlaneInfo>().CruiseAltitude;
					var plane = w.CreateActor(info.DeliveryAircraft, new TypeDictionary
					{
						new CenterPositionInit(startPos.CenterPosition + new WVec(WRange.Zero, WRange.Zero, altitude)),
						new OwnerInit(w.WorldActor.Owner),
						new FacingInit(Util.GetFacing(p - startPos, 0))
					});

					plane.CancelActivity();
					plane.QueueActivity(new FlyAttack(Target.FromCell(p)));
					plane.Trait<ParaDrop>().SetLZ(p);
					plane.Trait<Cargo>().Load(plane, crate);
				}
				else
				{
					w.CreateActor(crateActor, new TypeDictionary { new OwnerInit(w.WorldActor.Owner), new LocationInit(p) });
				}
			});
		}

		CPos? ChooseDropCell(Actor self, bool inWater, int maxTries)
		{
			for (var n = 0; n < maxTries; n++)
			{
				var p = self.World.Map.ChooseRandomCell(self.World.SharedRandom);

				// Is this valid terrain?
				var terrainType = self.World.Map.GetTerrainInfo(p).Type;
				if (!(inWater ? info.ValidWater : info.ValidGround).Contains(terrainType))
					continue;

				// Don't drop on any actors
				if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(p) != null
					|| self.World.ActorMap.GetUnitsAt(p).Any())
					continue;

				return p;
			}

			return null;
		}

		string ChooseCrateActor()
		{
			var crateShares = info.CrateActorShares;
			var n = self.World.SharedRandom.Next(crateShares.Sum());

			var cumulativeShares = 0;
			for (var i = 0; i < crateShares.Length; i++)
			{
				cumulativeShares += crateShares[i];
				if (n <= cumulativeShares)
					return info.CrateActors[i];
			}

			return null;
		}

		public void IncrementCrates()
		{
			crates++;
		}

		public void DecrementCrates()
		{
			crates--;
		}
	}
}
