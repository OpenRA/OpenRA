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
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Mods.RA.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class ParatroopersPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		public readonly string UnitType = "badr";
		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new WVec(-1536, 1536, 0);

		[Desc("Number of facings that the delivery aircraft may approach from.")]
		public readonly int QuantizedFacings = 32;

		[Desc("Spawn and remove the plane this far outside the map.")]
		public readonly WRange Cordon = new WRange(5120);

		[ActorReference]
		[Desc("Troops to be delivered.  They will be distributed between the planes if SquadSize > 1.")]
		public string[] DropItems = { };

		[Desc("Risks stuck units when they don't have the Paratrooper trait.")]
		public readonly bool AllowImpassableCells = false;

		[ActorReference]
		[Desc("Actor to spawn when the paradrop starts.")]
		public readonly string CameraActor = null;

		[Desc("Amount of time (in ticks) to keep the camera alive while the passengers drop.")]
		public readonly int CameraRemoveDelay = 85;

		[Desc("Weapon range offset to apply during the beacon clock calculation.")]
		public readonly WRange BeaconDistanceOffset = WRange.FromCells(4);

		public override object Create(ActorInitializer init) { return new ParatroopersPower(init, this); }
	}

	public class ParatroopersPower : SupportPower
	{
		public ParatroopersPower(ActorInitializer init, ParatroopersPowerInfo info) : base(init, info) { }

		public override void Activate(Order order)
		{
			base.Activate(order);

			var info = this.Info as ParatroopersPowerInfo;
			var dropFacing = Util.QuantizeFacing(Self.World.SharedRandom.Next(256), info.QuantizedFacings) * (256 / info.QuantizedFacings);
			var dropRotation = WRot.FromFacing(dropFacing);
			var delta = new WVec(0, -1024, 0).Rotate(dropRotation);

			var altitude = Self.World.Map.Rules.Actors[info.UnitType].Traits.Get<PlaneInfo>().CruiseAltitude.Range;
			var target = Self.World.Map.CenterOfCell(order.TargetLocation) + new WVec(0, 0, altitude);
			var startEdge = target - (Self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Range * delta / 1024;
			var finishEdge = target + (Self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Range * delta / 1024;

			Actor camera = null;
			Beacon beacon = null;
			var aircraftInRange = new Dictionary<Actor, bool>();

			Action<Actor> onEnterRange = a =>
			{
				// Spawn a camera and remove the beacon when the first plane enters the target area
				if (info.CameraActor != null && !aircraftInRange.Any(kv => kv.Value))
				{
					Self.World.AddFrameEndTask(w =>
					{
						camera = w.CreateActor(info.CameraActor, new TypeDictionary
						{
							new LocationInit(order.TargetLocation),
							new OwnerInit(Self.Owner),
						});
					});
				}

				if (beacon != null)
				{
					Self.World.AddFrameEndTask(w =>
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
				}
			};

			Self.World.AddFrameEndTask(w =>
			{
				var notification = Self.Owner.IsAlliedWith(Self.World.RenderPlayer) ? info.LaunchSound : info.IncomingSound;
				Sound.Play(notification);

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
						new OwnerInit(Self.Owner),
						new FacingInit(dropFacing),
					});

					var drop = a.Trait<ParaDrop>();
					drop.SetLZ(w.Map.CellContaining(target + targetOffset), !info.AllowImpassableCells);
					drop.OnEnteredDropRange += onEnterRange;
					drop.OnExitedDropRange += onExitRange;
					drop.OnRemovedFromWorld += onExitRange;

					var cargo = a.Trait<Cargo>();
					var passengers = info.DropItems.Skip(added).Take(passengersPerPlane);
					added += passengersPerPlane;

					foreach (var p in passengers)
						cargo.Load(a, Self.World.CreateActor(false, p.ToLowerInvariant(),
							new TypeDictionary { new OwnerInit(a.Owner) }));

					a.QueueActivity(new Fly(a, Target.FromPos(finishEdge + spawnOffset)));
					a.QueueActivity(new RemoveSelf());
					aircraftInRange.Add(a, false);
					distanceTestActor = a;
				}

				if (info.DisplayBeacon)
				{
					var distance = (target - startEdge).HorizontalLength;

					beacon = new Beacon(
						order.Player,
						Self.World.Map.CenterOfCell(order.TargetLocation),
						info.BeaconPalettePrefix,
						info.BeaconPoster,
						info.BeaconPosterPalette,
						() => 1 - ((distanceTestActor.CenterPosition - target).HorizontalLength - info.BeaconDistanceOffset.Range) * 1f / distance
					);

					w.Add(beacon);
				}
			});
		}
	}
}
