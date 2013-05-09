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
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CrateDropInfo : ITraitInfo
	{
		[Desc("Minimum number of crates")]
		public readonly int Minimum = 1;
		[Desc("Maximum number of crates")]
		public readonly int Maximum = 255;
		[Desc("Which terrain types can we drop on?")]
		public readonly string[] ValidGround = {"Clear", "Rough", "Road", "Ore", "Beach"};
		[Desc("Which terrain types count as water?")]
		public readonly string[] ValidWater = {"Water"};
		[Desc("Average time (seconds) between crate spawn")]
		public readonly int SpawnInterval = 180;
		[Desc("Chance of generating a water crate instead of a land crate")]
		public readonly float WaterChance = .2f;
		[ActorReference]
		public readonly string CrateActor = "crate";
		[ActorReference]
		public readonly string DeliveryAircraft = "badr";

		public object Create (ActorInitializer init) { return new CrateDrop(this); }
	}

	public class CrateDrop : ITick
	{
		List<Actor> crates = new List<Actor>();
		int ticks = 0;
		CrateDropInfo Info;

		public CrateDrop(CrateDropInfo info) { Info = info; }

		public void Tick(Actor self)
		{
			if (!self.World.LobbyInfo.GlobalSettings.Crates) return;

			if (--ticks <= 0)
			{
				ticks = Info.SpawnInterval * 25;		// TODO: randomize

				crates.RemoveAll(x => !x.IsInWorld);	// BUG: this removes crates that are cargo of a BADR!

				var toSpawn = Math.Max(0, Info.Minimum - crates.Count)
					+ (crates.Count < Info.Maximum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnCrate(self);
			}
		}

		CPos? ChooseDropCell(Actor self, bool inWater, int maxTries)
		{
			for( var n = 0; n < maxTries; n++ )
			{
				var p = self.World.ChooseRandomCell(self.World.SharedRandom);

				// Is this valid terrain?
				var terrainType = self.World.GetTerrainType(p);
				if (!(inWater ? Info.ValidWater : Info.ValidGround).Contains(terrainType)) continue;

				// Don't drop on any actors
				if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(p) != null) continue;
				if (self.World.ActorMap.GetUnitsAt(p).Any()) continue;

				return p;
			}

			return null;
		}

		void SpawnCrate(Actor self)
		{
			var inWater = self.World.SharedRandom.NextFloat() < Info.WaterChance;
			var pp = ChooseDropCell(self, inWater, 100);
			if (pp == null)	return;

			var p = pp.Value;	//
			self.World.AddFrameEndTask(w =>
				{
					var crate = w.CreateActor(false, Info.CrateActor, new TypeDictionary { new OwnerInit(w.WorldActor.Owner) });
					crates.Add(crate);

					var startPos = w.ChooseRandomEdgeCell();
					var plane = w.CreateActor(Info.DeliveryAircraft, new TypeDictionary
					{
						new LocationInit( startPos ),
						new OwnerInit( w.WorldActor.Owner),
						new FacingInit( Util.GetFacing(p - startPos, 0) ),
						new AltitudeInit( Rules.Info[Info.DeliveryAircraft].Traits.Get<AircraftInfo>().CruiseAltitude ),
					});

					plane.CancelActivity();
					plane.QueueActivity(new FlyAttack(Target.FromCell(p)));
					plane.Trait<ParaDrop>().SetLZ(p);
					plane.Trait<Cargo>().Load(plane, crate);
				});
		}
	}
}
