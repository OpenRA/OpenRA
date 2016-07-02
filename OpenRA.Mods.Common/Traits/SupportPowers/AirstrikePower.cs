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
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AirstrikePowerInfo : SupportPowerInfo
	{
		[ActorReference(typeof(AircraftInfo))]
		public readonly string UnitType = "badr.bomber";
		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new WVec(-1536, 1536, 0);

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new WDist(5120);

		[ActorReference]
		[Desc("Actor to spawn when the aircraft start attacking")]
		public readonly string CameraActor = null;

		[Desc("Amount of time to keep the camera alive after the aircraft have finished attacking")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("Weapon range offset to apply during the beacon clock calculation")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(6);

		public override object Create(ActorInitializer init) { return new AirstrikePower(init.Self, this); }
	}

	public class AirstrikePower : SupportPower
	{
		public AirstrikePower(Actor self, AirstrikePowerInfo info)
			: base(self, info) { }

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			SendAirstrike(self, self.World.Map.CenterOfCell(order.TargetLocation));
		}

		public void SendAirstrike(Actor self, WPos target, bool randomize = true, int attackFacing = 0)
		{
			var info = Info as AirstrikePowerInfo;

			if (randomize)
				attackFacing = 256 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings;

			var altitude = self.World.Map.Rules.Actors[info.UnitType].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var attackRotation = WRot.FromFacing(attackFacing);
			var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
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

			self.World.AddFrameEndTask(w =>
			{
				var isAllied = self.Owner.IsAlliedWith(self.World.RenderPlayer);
				Game.Sound.Play(isAllied ? Info.LaunchSound : Info.IncomingSound);

				var speech = isAllied ? Info.LaunchSpeechNotification : Info.IncomingSpeechNotification;
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech", speech, self.Owner.Faction.InternalName);

				Actor distanceTestActor = null;
				for (var i = -info.SquadSize / 2; i <= info.SquadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (info.SquadSize & 1) == 0)
						continue;

					// Includes the 90 degree rotation between body and world coordinates
					var so = info.SquadOffset;
					var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);
					var targetOffset = new WVec(i * so.Y, 0, 0).Rotate(attackRotation);

					var a = w.CreateActor(info.UnitType, new TypeDictionary
					{
						new CenterPositionInit(startEdge + spawnOffset),
						new OwnerInit(self.Owner),
						new FacingInit(attackFacing),
					});

					var attack = a.Trait<AttackBomber>();
					attack.SetTarget(w, target + targetOffset);
					attack.OnEnteredAttackRange += onEnterRange;
					attack.OnExitedAttackRange += onExitRange;
					attack.OnRemovedFromWorld += onExitRange;

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
		}
	}
}
