#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Air;
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
		[Desc("Drop crates via DeliveryAircraft: or instantly spawn them on the ground")]
		public readonly bool DeliverByAircraft = false;
		[Desc("If DeliverByAircraft: yes, this actor will deliver crates"), ActorReference]
		public readonly string DeliveryAircraft = "badr";
		[Desc("Crate actor to drop"), ActorReference]
		public readonly string CrateActor = "crate";

		public object Create(ActorInitializer init) { return new CrateSpawner(this); }
	}

	public class CrateSpawner : ITick
	{
		List<Actor> crates = new List<Actor>();
		int ticks = 0;
		CrateSpawnerInfo Info;

		public CrateSpawner(CrateSpawnerInfo info) { Info = info; }

		public void Tick(Actor self)
		{
			if (!self.World.LobbyInfo.GlobalSettings.Crates)
				return;

			if (--ticks <= 0)
			{
				ticks = Info.SpawnInterval * 25;

				crates.RemoveAll(x => !x.IsInWorld); // BUG: this removes crates that are cargo of a BADR!

				var toSpawn = Math.Max(0, Info.Minimum - crates.Count)
					+ (crates.Count < Info.Maximum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnCrate(self);
			}
		}

		void SpawnCrate(Actor self)
		{
			var threshold = 100;
			var inWater = self.World.SharedRandom.NextFloat() < Info.WaterChance;
			var pp = ChooseDropCell(self, inWater, threshold);

			if (pp == null)
				return;

			var p = pp.Value;

			self.World.AddFrameEndTask(w =>
			{
				if (Info.DeliverByAircraft)
				{
					var crate = w.CreateActor(false, Info.CrateActor, new TypeDictionary { new OwnerInit(w.WorldActor.Owner) });
					crates.Add(crate);

					var startPos = w.ChooseRandomEdgeCell();
					var plane = w.CreateActor(Info.DeliveryAircraft, new TypeDictionary
					{
						new LocationInit(startPos),
						new OwnerInit(w.WorldActor.Owner),
						new FacingInit(Util.GetFacing(p - startPos, 0)),
						new AltitudeInit(Rules.Info[Info.DeliveryAircraft].Traits.Get<AircraftInfo>().CruiseAltitude),
					});

					plane.CancelActivity();
					plane.QueueActivity(new FlyAttack(Target.FromCell(p)));
					plane.Trait<ParaDrop>().SetLZ(p);
					plane.Trait<Cargo>().Load(plane, crate);
				}
				else
				{
					crates.Add(w.CreateActor(Info.CrateActor, new TypeDictionary { new OwnerInit(w.WorldActor.Owner), new LocationInit(p) }));
				}
			});
		}

		CPos? ChooseDropCell(Actor self, bool inWater, int maxTries)
		{
			for (var n = 0; n < maxTries; n++)
			{
				var p = self.World.ChooseRandomCell(self.World.SharedRandom);

				// Is this valid terrain?
				var terrainType = self.World.GetTerrainType(p);
				if (!(inWater ? Info.ValidWater : Info.ValidGround).Contains(terrainType))
					continue;

				// Don't drop on any actors
				if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(p) != null
					|| self.World.ActorMap.GetUnitsAt(p).Any())
					continue;

				return p;
			}

			return null;
		}
	}
}
