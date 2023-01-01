#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.World)]
	public class CrateSpawnerInfo : TraitInfo, ILobbyOptions
	{
		[TranslationReference]
		[Desc("Descriptive label for the crates checkbox in the lobby.")]
		public readonly string CheckboxLabel = "checkbox-crates.label";

		[TranslationReference]
		[Desc("Tooltip description for the crates checkbox in the lobby.")]
		public readonly string CheckboxDescription = "checkbox-crates.description";

		[Desc("Default value of the crates checkbox in the lobby.")]
		public readonly bool CheckboxEnabled = true;

		[Desc("Prevent the crates state from being changed in the lobby.")]
		public readonly bool CheckboxLocked = false;

		[Desc("Whether to display the crates checkbox in the lobby.")]
		public readonly bool CheckboxVisible = true;

		[Desc("Display order for the crates checkbox in the lobby.")]
		public readonly int CheckboxDisplayOrder = 0;

		[Desc("Minimum number of crates.")]
		public readonly int Minimum = 1;

		[Desc("Maximum number of crates.")]
		public readonly int Maximum = 255;

		[Desc("Average time (ticks) between crate spawn.")]
		public readonly int SpawnInterval = 4500;

		[Desc("Delay (in ticks) before the first crate spawns.")]
		public readonly int InitialSpawnDelay = 0;

		[Desc("Which terrain types can we drop on?")]
		public readonly HashSet<string> ValidGround = new HashSet<string> { "Clear", "Rough", "Road", "Ore", "Beach" };

		[Desc("Which terrain types count as water?")]
		public readonly HashSet<string> ValidWater = new HashSet<string> { "Water" };

		[Desc("Chance of generating a water crate instead of a land crate.")]
		public readonly int WaterChance = 20;

		[ActorReference]
		[Desc("Crate actors to drop.")]
		public readonly string[] CrateActors = { "crate" };

		[Desc("Chance of each crate actor spawning.")]
		public readonly int[] CrateActorShares = { 10 };

		[ActorReference]
		[Desc("If a DeliveryAircraft: is specified, then this actor will deliver crates.")]
		public readonly string DeliveryAircraft = null;

		[Desc("Number of facings that the delivery aircraft may approach from.")]
		public readonly int QuantizedFacings = 32;

		[Desc("Spawn and remove the plane this far outside the map.")]
		public readonly WDist Cordon = new WDist(5120);

		IEnumerable<LobbyOption> ILobbyOptions.LobbyOptions(MapPreview map)
		{
			yield return new LobbyBooleanOption("crates", CheckboxLabel, CheckboxDescription, CheckboxVisible, CheckboxDisplayOrder, CheckboxEnabled, CheckboxLocked);
		}

		public override object Create(ActorInitializer init) { return new CrateSpawner(init.Self, this); }
	}

	public class CrateSpawner : ITick, INotifyCreated
	{
		readonly Actor self;
		readonly CrateSpawnerInfo info;
		bool enabled;
		int crates;
		int ticks;

		public CrateSpawner(Actor self, CrateSpawnerInfo info)
		{
			this.self = self;
			this.info = info;

			ticks = info.InitialSpawnDelay;
		}

		void INotifyCreated.Created(Actor self)
		{
			enabled = self.World.LobbyInfo.GlobalSettings
				.OptionOrDefault("crates", info.CheckboxEnabled);
		}

		void ITick.Tick(Actor self)
		{
			if (!enabled)
				return;

			if (--ticks <= 0)
			{
				ticks = info.SpawnInterval;

				var toSpawn = Math.Max(0, info.Minimum - crates)
					+ (crates < info.Maximum && info.Maximum > info.Minimum ? 1 : 0);

				for (var n = 0; n < toSpawn; n++)
					SpawnCrate(self);
			}
		}

		void SpawnCrate(Actor self)
		{
			var inWater = self.World.SharedRandom.Next(100) < info.WaterChance;
			var pp = ChooseDropCell(self, inWater, 100);

			if (pp == null)
				return;

			var p = pp.Value;
			var crateActor = ChooseCrateActor();

			self.World.AddFrameEndTask(w =>
			{
				if (info.DeliveryAircraft != null)
				{
					var crate = w.CreateActor(false, crateActor, new TypeDictionary { new OwnerInit(w.WorldActor.Owner) });
					var dropFacing = new WAngle(1024 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings);
					var delta = new WVec(0, -1024, 0).Rotate(WRot.FromYaw(dropFacing));

					var altitude = self.World.Map.Rules.Actors[info.DeliveryAircraft].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
					var target = self.World.Map.CenterOfCell(p) + new WVec(0, 0, altitude);
					var startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;
					var finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Length * delta / 1024;

					var plane = w.CreateActor(info.DeliveryAircraft, new TypeDictionary
					{
						new CenterPositionInit(startEdge),
						new OwnerInit(self.Owner),
						new FacingInit(dropFacing),
					});

					var drop = plane.Trait<ParaDrop>();
					drop.SetLZ(p, true);
					plane.Trait<Cargo>().Load(plane, crate);

					plane.QueueActivity(false, new Fly(plane, Target.FromPos(finishEdge)));
					plane.QueueActivity(new RemoveSelf());
				}
				else
					w.CreateActor(crateActor, new TypeDictionary { new OwnerInit(w.WorldActor.Owner), new LocationInit(p) });
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
				if (self.World.ActorMap.GetActorsAt(p).Any())
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
