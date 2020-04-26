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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class AirstrikePowerSquadMember
	{
		public readonly string UnitType;

		public readonly WVec SpawnOffset;

		public readonly WVec TargetOffset;

		public AirstrikePowerSquadMember(MiniYamlNode yamlNode)
		{
			FieldLoader.Load(this, yamlNode.Value);
		}
	}

	[Desc("Support power that spawns a group of aircraft and orders them to deliver an airstrike.")]
	public class AirstrikePowerInfo : DirectionalSupportPowerInfo
	{
		[FieldLoader.LoadUsing(nameof(LoadSquad))]
		[Desc("A list of aircraft in the squad. Each has configurable "
			+ nameof(AirstrikePowerSquadMember.UnitType) + ", "
			+ nameof(AirstrikePowerSquadMember.SpawnOffset) + " and "
			+ nameof(AirstrikePowerSquadMember.TargetOffset))]
		public readonly List<AirstrikePowerSquadMember> Squad = [];

		[Desc("Number of different possible facings of the aircraft (used only for choosing a random direction to spawn from.)")]
		public readonly int QuantizedFacings = 32;

		[Desc("Additional distance from the map edge to spawn the aircraft.")]
		public readonly WDist Cordon = new(5120);

		[ActorReference]
		[Desc("Actor to spawn when the aircraft start attacking.")]
		public readonly string CameraActor = null;

		[Desc("Amount of time to keep the camera alive after the aircraft have finished attacking.")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("Weapon range offset to apply during the beacon clock calculation.")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(6);

		static object LoadSquad(MiniYaml yaml)
		{
			var ret = new List<AirstrikePowerSquadMember>();

			var squadNode = yaml.Nodes.Single(n => n.Key == "Squad");
			foreach (var d in squadNode.Value.Nodes)
				ret.Add(new AirstrikePowerSquadMember(d));

			return ret;
		}

		// This property is specially added so we can still lint-check the actor types.
		[ActorReference(typeof(AircraftInfo))]
		public IEnumerable<string> LintSquadActors => Squad.Select(s => s.UnitType);

		public override object Create(ActorInitializer init) { return new AirstrikePower(init.Self, this); }
	}

	public class AirstrikePower : DirectionalSupportPower
	{
		readonly AirstrikePowerInfo info;

		public AirstrikePower(Actor self, AirstrikePowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			var facing = info.UseDirectionalTarget && order.ExtraData != uint.MaxValue ? (WAngle?)WAngle.FromFacing((int)order.ExtraData) : null;
			SendAirstrike(self, order.Target.CenterPosition, facing);
		}

		public Actor[] SendAirstrike(Actor self, WPos target, WAngle? facing = null)
		{
			var aircraft = new List<Actor>();
			if (!facing.HasValue)
				facing = new WAngle(1024 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings);

			Actor camera = null;
			Beacon beacon = null;
			var aircraftInRange = new Dictionary<Actor, bool>();

			void OnEnterRange(Actor a)
			{
				// Spawn a camera and remove the beacon when the first plane enters the target area
				if (info.CameraActor != null && camera == null && !aircraftInRange.Any(kv => kv.Value))
				{
					self.World.AddFrameEndTask(w =>
					{
						camera = w.CreateActor(info.CameraActor,
						[
							new LocationInit(self.World.Map.CellContaining(target)),
							new OwnerInit(self.Owner),
						]);
					});
				}

				RemoveBeacon(beacon);

				aircraftInRange[a] = true;
			}

			void OnExitRange(Actor a)
			{
				aircraftInRange[a] = false;

				// Remove the camera when the final plane leaves the target area
				if (!aircraftInRange.Any(kv => kv.Value))
					RemoveCamera(camera);
			}

			void OnRemovedFromWorld(Actor a)
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
			}

			WPos? startPos = null;

			// Create the actors immediately so they can be returned.
			foreach (var squadMember in info.Squad)
			{
				var a = self.World.CreateActor(false, squadMember.UnitType,
				[
					new OwnerInit(self.Owner),
					new FacingInit(facing.Value),
				]);

				aircraft.Add(a);
				aircraftInRange.Add(a, false);
			}

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				Actor distanceTestActor = null;
				for (var i = 0; i < aircraft.Count; i++)
				{
					var squadMember = info.Squad[i];
					var actor = aircraft[i];

					var altitude = self.World.Map.Rules.Actors[squadMember.UnitType].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
					var attackRotation = WRot.FromYaw(facing.Value);
					var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
					var targetPos = target + new WVec(0, 0, altitude);
					var startEdge = targetPos - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;
					var finishEdge = targetPos + (self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Length * delta / 1024;

					startPos = startEdge;

					// Includes the 90 degree rotation between body and world coordinates.
					var so = squadMember.SpawnOffset;
					var to = squadMember.TargetOffset;
					var spawnOffset = new WVec(so.Y, -1 * so.X, 0).Rotate(attackRotation);
					var targetOffset = new WVec(to.Y, 0, 0).Rotate(attackRotation);

					actor.Trait<IPositionable>().SetPosition(actor, startEdge + spawnOffset);
					w.Add(actor);

					var attack = actor.Trait<AttackBomber>();
					attack.SetTarget(targetPos + targetOffset);
					attack.OnEnteredAttackRange += OnEnterRange;
					attack.OnExitedAttackRange += OnExitRange;
					attack.OnRemovedFromWorld += OnRemovedFromWorld;

					actor.QueueActivity(new Fly(actor, Target.FromPos(target + targetOffset)));
					actor.QueueActivity(new Fly(actor, Target.FromPos(finishEdge + spawnOffset)));
					actor.QueueActivity(new RemoveSelf());
					distanceTestActor = actor;
				}

				if (Info.DisplayBeacon && startPos.HasValue)
				{
					var distance = (target - startPos.Value).HorizontalLength;

					beacon = new Beacon(
						self.Owner,
						new WPos(target.X, target.Y, 0),
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
		}

		void RemoveBeacon(Beacon beacon)
		{
			if (beacon == null)
				return;

			Self.World.AddFrameEndTask(w => w.Remove(beacon));
		}
	}
}
