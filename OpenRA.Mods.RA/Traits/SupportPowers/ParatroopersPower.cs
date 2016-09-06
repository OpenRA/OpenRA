#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Traits
{
	public class ParatroopersPowerInfo : SupportPowerInfo
	{
		[ActorReference(typeof(AircraftInfo))]
		public readonly string UnitType = "badr";
		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new WVec(-1536, 1536, 0);

		[Desc("Number of facings that the delivery aircraft may approach from.")]
		public readonly int QuantizedFacings = 32;

		[Desc("Spawn and remove the plane this far outside the map.")]
		public readonly WDist Cordon = new WDist(5120);

		[ActorReference(typeof(PassengerInfo))]
		[Desc("Troops to be delivered.  They will be distributed between the planes if SquadSize > 1.")]
		public readonly string[] DropItems = { };

		[Desc("Risks stuck units when they don't have the Paratrooper trait.")]
		public readonly bool AllowImpassableCells = false;

		[ActorReference]
		[Desc("Actor to spawn when the paradrop starts.")]
		public readonly string CameraActor = null;

		[Desc("Amount of time (in ticks) to keep the camera alive while the passengers drop.")]
		public readonly int CameraRemoveDelay = 85;

		[Desc("Weapon range offset to apply during the beacon clock calculation.")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(4);

		public override object Create(ActorInitializer init) { return new ParatroopersPower(init.Self, this); }
	}

	public class ParatroopersPower : SupportPower
	{
		public ParatroopersPower(Actor self, ParatroopersPowerInfo info) : base(self, info) { }

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			SendParatroopers(self, self.World.Map.CenterOfCell(order.TargetLocation));
		}

		public Actor[] SendParatroopers(Actor self, WPos target, bool randomize = true, int dropFacing = 0)
		{
			var units = new List<Actor>();

			var info = Info as ParatroopersPowerInfo;

			if (randomize)
				dropFacing = 256 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings;

			var utLower = info.UnitType.ToLowerInvariant();
			ActorInfo unitType;
			if (!self.World.Map.Rules.Actors.TryGetValue(utLower, out unitType))
				throw new YamlException("Actors ruleset does not include the entry '{0}'".F(utLower));

			var altitude = unitType.TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var dropRotation = WRot.FromFacing(dropFacing);
			var delta = new WVec(0, -1024, 0).Rotate(dropRotation);
			target = target + new WVec(0, 0, altitude);
			var startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;
			var finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Length * delta / 1024;

			Actor camera = null;
			Beacon beacon = null;
			var aircraftInRange = new Dictionary<Actor, bool>();

			Action<Actor> onEnterRange = a =>
			{
				// Spawn a camera and remove the beacon when the first plane enters the target area
				if (info.CameraActor != null && !aircraftInRange.Any(kv => kv.Value))
				{
					self.World.AddFrameEndTask(w =>
					{
						camera = w.CreateActor(info.CameraActor, new TypeDictionary
						{
							new LocationInit(self.World.Map.CellContaining(target)),
							new OwnerInit(self.Owner),
						});
					});
				}

				if (beacon != null)
				{
					self.World.AddFrameEndTask(w =>
					{
						w.Remove(beacon);
						beacon = null;
					});
				}

				aircraftInRange[a] = true;
			};

			Action<Actor> onExitRange = a =>
			{
				aircraftInRange[a] = false;

				// Remove the camera when the final plane leaves the target area
				if (!aircraftInRange.Any(kv => kv.Value))
				{
					if (camera != null)
					{
						camera.QueueActivity(new Wait(info.CameraRemoveDelay));
						camera.QueueActivity(new RemoveSelf());
					}

					camera = null;

					if (beacon != null)
					{
						self.World.AddFrameEndTask(w =>
						{
							w.Remove(beacon);
							beacon = null;
						});
					}
				}
			};

			foreach (var p in info.DropItems)
			{
				var unit = self.World.CreateActor(false, p.ToLowerInvariant(),
					new TypeDictionary { new OwnerInit(self.Owner) });

				units.Add(unit);
			}

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				Actor distanceTestActor = null;

				var passengersPerPlane = (info.DropItems.Length + info.SquadSize - 1) / info.SquadSize;
				var added = 0;
				for (var i = -info.SquadSize / 2; i <= info.SquadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (info.SquadSize & 1) == 0)
						continue;

					// Includes the 90 degree rotation between body and world coordinates
					var so = info.SquadOffset;
					var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(dropRotation);
					var targetOffset = new WVec(i * so.Y, 0, 0).Rotate(dropRotation);

					var a = w.CreateActor(info.UnitType, new TypeDictionary
					{
						new CenterPositionInit(startEdge + spawnOffset),
						new OwnerInit(self.Owner),
						new FacingInit(dropFacing),
					});

					var drop = a.Trait<ParaDrop>();
					drop.SetLZ(w.Map.CellContaining(target + targetOffset), !info.AllowImpassableCells);
					drop.OnEnteredDropRange += onEnterRange;
					drop.OnExitedDropRange += onExitRange;
					drop.OnRemovedFromWorld += onExitRange;

					var cargo = a.Trait<Cargo>();
					var passengers = units.Skip(added).Take(passengersPerPlane);
					added += passengersPerPlane;

					foreach (var p in passengers)
						cargo.Load(a, p);

					a.QueueActivity(new Fly(a, Target.FromPos(target + spawnOffset)));
					a.QueueActivity(new Fly(a, Target.FromPos(finishEdge + spawnOffset)));
					a.QueueActivity(new RemoveSelf());
					aircraftInRange.Add(a, false);
					distanceTestActor = a;
				}

				if (Info.DisplayBeacon)
				{
					var distance = (target - startEdge).HorizontalLength;

					beacon = new Beacon(
						self.Owner,
						target - new WVec(0, 0, altitude),
						Info.BeaconPaletteIsPlayerPalette,
						Info.BeaconPalette,
						Info.BeaconImage,
						Info.BeaconPoster,
						Info.BeaconPosterPalette,
						Info.ArrowSequence,
						Info.CircleSequence,
						Info.ClockSequence,
						() => 1 - ((distanceTestActor.CenterPosition - target).HorizontalLength - info.BeaconDistanceOffset.Length) * 1f / distance);

					w.Add(beacon);
				}
			});

			return units.ToArray();
		}
	}
}
