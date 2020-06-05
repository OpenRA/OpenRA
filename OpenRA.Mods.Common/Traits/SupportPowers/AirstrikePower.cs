#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AirstrikePowerInfo : SupportPowerInfo
	{
		[ActorReference(typeof(AircraftInfo))]
		[Desc("Aircraft used to deliver the airstrike.")]
		public readonly string UnitType = "badr.bomber";

		[Desc("Number of aircraft to use in an airstrike formation.")]
		public readonly int SquadSize = 1;

		[Desc("Offset vector between the aircraft in a formation.")]
		public readonly WVec SquadOffset = new WVec(-1536, 1536, 0);

		[Desc("Number of different possible facings of the aircraft (used only for choosing a random direction to spawn from.)")]
		public readonly int QuantizedFacings = 32;

		[Desc("Additional distance from the map edge to spawn and despawn the aircraft.")]
		public readonly WDist Cordon = new WDist(5120);

		[ActorReference]
		[Desc("Actor to spawn when the aircraft start attacking")]
		public readonly string CameraActor = null;

		[Desc("Amount of time to keep the camera alive after the aircraft have finished attacking")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("The aircraft will spawn at the player baseline (map edge directly behind the player spawn)")]
		public readonly bool BaselineSpawn = false;

		[Desc("Enables the player directional targeting")]
		public readonly bool UseDirectionalTarget = false;

		[Desc("Animation used to render the direction arrows.")]
		public readonly string DirectionArrowAnimation = null;

		[Desc("Palette for direction cursor animation.")]
		public readonly string DirectionArrowPalette = "chrome";

		[Desc("Weapon range offset to apply during the beacon clock calculation")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(6);

		public override object Create(ActorInitializer init) { return new AirstrikePower(init.Self, this); }
	}

	public class AirstrikePower : SupportPower
	{
		readonly AirstrikePowerInfo info;
		readonly MPStartLocations mpStart;
		readonly WVec altitude;

		public AirstrikePower(Actor self, AirstrikePowerInfo info)
			: base(self, info)
		{
			this.info = info;
			var aircraftInfo = self.World.Map.Rules.Actors[info.UnitType].TraitInfoOrDefault<AircraftInfo>();
			altitude = new WVec(WDist.Zero, WDist.Zero, aircraftInfo.CruiseAltitude);
			mpStart = self.World.WorldActor.TraitOrDefault<MPStartLocations>();
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			if (info.UseDirectionalTarget)
			{
				Game.Sound.PlayToPlayer(SoundType.UI, manager.Self.Owner, Info.SelectTargetSound);
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					Info.SelectTargetSpeechNotification, self.Owner.Faction.InternalName);

				self.World.OrderGenerator = new SelectDirectionalTarget(self.World, order, manager, Info.Cursor, info.DirectionArrowAnimation, info.DirectionArrowPalette);
			}
			else
				base.SelectTarget(self, order, manager);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var facing = info.UseDirectionalTarget && order.ExtraData != uint.MaxValue ? (WAngle?)WAngle.FromFacing((int)order.ExtraData) : null;
			SendAirstrike(self, order.Target.CenterPosition, facing);
		}

		public Actor[] SendAirstrike(Actor self, WPos target, WAngle? facing = null)
		{
			WPos startEdge;
			WPos finishEdge;
			WRot attackRotation;
			WVec delta;

			if (info.BaselineSpawn && mpStart != null)
			{
				var spawnPos = self.World.Map.CenterOfCell(mpStart.Start[self.Owner]);
				var bounds = self.World.Map.Bounds;
				var spawnVec = spawnPos - self.World.Map.CenterOfCell(new MPos(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2).ToCPos(self.World.Map));
				startEdge = spawnPos + (self.World.Map.DistanceToEdge(spawnPos, spawnVec) + info.Cordon).Length * spawnVec / spawnVec.Length;
				facing = (target - startEdge).Yaw;
				attackRotation = WRot.FromYaw(facing.Value);
				delta = new WVec(0, -1024, 0).Rotate(attackRotation);
			}
			else
			{
				if (!facing.HasValue)
					facing = new WAngle(1024 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings);

				attackRotation = WRot.FromYaw(facing.Value);
				delta = new WVec(0, -1024, 0).Rotate(attackRotation);
				startEdge = target - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;
			}

			target += altitude;
			startEdge += altitude;
			finishEdge = finishEdge = target + (self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Length * delta / 1024;

			Actor camera = null;
			Beacon beacon = null;
			var aircraftInRange = new Dictionary<Actor, bool>();

			Action<Actor> onEnterRange = a =>
			{
				// Spawn a camera and remove the beacon when the first plane enters the target area
				if (info.CameraActor != null && camera == null && !aircraftInRange.Any(kv => kv.Value))
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

				RemoveBeacon(beacon);

				aircraftInRange[a] = true;
			};

			Action<Actor> onExitRange = a =>
			{
				aircraftInRange[a] = false;

				// Remove the camera when the final plane leaves the target area
				if (!aircraftInRange.Any(kv => kv.Value))
					RemoveCamera(camera);
			};

			Action<Actor> onRemovedFromWorld = a =>
			{
				aircraftInRange[a] = false;

				// Checking for attack range is not relevant here because
				// aircraft may be shot down before entering the range.
				// If at the map's edge, they may be removed from world before leaving.
				if (aircraftInRange.All(kv => !kv.Key.IsInWorld))
				{
					RemoveCamera(camera);
					RemoveBeacon(beacon);
				}
			};

			// Create the actors immediately so they can be returned
			var aircraft = new List<Actor>();
			for (var i = -info.SquadSize / 2; i <= info.SquadSize / 2; i++)
			{
				// Even-sized squads skip the lead plane
				if (i == 0 && (info.SquadSize & 1) == 0)
					continue;

				// Includes the 90 degree rotation between body and world coordinates
				var so = info.SquadOffset;
				var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);
				var targetOffset = new WVec(i * so.Y, 0, 0).Rotate(attackRotation);
				var a = self.World.CreateActor(false, info.UnitType, new TypeDictionary
				{
					new CenterPositionInit(startEdge + spawnOffset),
					new OwnerInit(self.Owner),
					new FacingInit(facing.Value),
				});

				aircraft.Add(a);
				aircraftInRange.Add(a, false);

				var attack = a.Trait<AttackBomber>();
				attack.SetTarget(self.World, target + targetOffset);
				attack.OnEnteredAttackRange += onEnterRange;
				attack.OnExitedAttackRange += onExitRange;
				attack.OnRemovedFromWorld += onRemovedFromWorld;
			}

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				var j = 0;
				Actor distanceTestActor = null;
				for (var i = -info.SquadSize / 2; i <= info.SquadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (info.SquadSize & 1) == 0)
						continue;

					// Includes the 90 degree rotation between body and world coordinates
					var so = info.SquadOffset;
					var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);

					var a = aircraft[j++];
					w.Add(a);

					a.QueueActivity(new Fly(a, Target.FromPos(target + spawnOffset)));
					a.QueueActivity(new Fly(a, Target.FromPos(finishEdge + spawnOffset)));
					a.QueueActivity(new RemoveSelf());
					distanceTestActor = a;
				}

				if (Info.DisplayBeacon)
				{
					var distance = (target - startEdge).HorizontalLength;

					beacon = new Beacon(
						self.Owner,
						target - altitude,
						Info.BeaconPaletteIsPlayerPalette,
						Info.BeaconPalette,
						Info.BeaconImage,
						Info.BeaconPoster,
						Info.BeaconPosterPalette,
						Info.BeaconSequence,
						Info.ArrowSequence,
						Info.CircleSequence,
						Info.ClockSequence,
						() => 1 - ((distanceTestActor.CenterPosition - target).HorizontalLength - info.BeaconDistanceOffset.Length) * 1f / distance,
						Info.BeaconDelay);

					w.Add(beacon);
				}
			});

			return aircraft.ToArray();
		}

		void RemoveCamera(Actor camera)
		{
			if (camera == null)
				return;

			camera.QueueActivity(new Wait(info.CameraRemoveDelay));
			camera.QueueActivity(new RemoveSelf());
			camera = null;
		}

		void RemoveBeacon(Beacon beacon)
		{
			if (beacon == null)
				return;

			Self.World.AddFrameEndTask(w =>
			{
				w.Remove(beacon);
				beacon = null;
			});
		}
	}
}
