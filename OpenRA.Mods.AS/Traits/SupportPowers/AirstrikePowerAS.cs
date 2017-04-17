#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Mods.AS.Activities;
using OpenRA.Mods.AS.Effects;
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	public enum AirstrikeMission { Attack, Guard }

	public class AirstrikePowerASInfo : SupportPowerInfo
	{
		[ActorReference(typeof(AircraftInfo))]
		public readonly string UnitType = "badr.bomber";
		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new WVec(-1536, 1536, 0);

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new WDist(5120);

		[ActorReference]
		[Desc("Actor to spawn when the aircrafts arrive.")]
		public readonly string CameraActor = null;

		[Desc("Amount of time to keep the camera alive after the aircraft have left the area.")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("Weapon range offset to apply during the beacon clock calculation.")]
		public readonly WDist BeaconDistanceOffset = WDist.FromCells(6);

		public readonly AirstrikeMission Mission = AirstrikeMission.Attack;

		public readonly int GuardDuration = 150;

		public override object Create(ActorInitializer init) { return new AirstrikePowerAS(init.Self, this); }
	}

	public class AirstrikePowerAS : SupportPower
	{
		public AirstrikePowerAS(Actor self, AirstrikePowerASInfo info)
			: base(self, info) { }

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			SendAirstrike(self, self.World.Map.CenterOfCell(order.TargetLocation));
		}

		public void SendAirstrike(Actor self, WPos target, bool randomize = true, int attackFacing = 0)
		{
			var info = Info as AirstrikePowerASInfo;

			if (randomize)
				attackFacing = 256 * self.World.SharedRandom.Next(info.QuantizedFacings) / info.QuantizedFacings;

			var altitude = self.World.Map.Rules.Actors[info.UnitType].TraitInfo<AircraftInfo>().CruiseAltitude.Length;
			var attackRotation = WRot.FromFacing(attackFacing);
			var delta = new WVec(0, -1024, 0).Rotate(attackRotation);
			target = target + new WVec(0, 0, altitude);

			var startPos = target - (self.World.Map.DistanceToEdge(target, -delta) + info.Cordon).Length * delta / 1024;

			self.World.AddFrameEndTask(w =>
			{
				PlayLaunchSounds();

				var aircrafts = new HashSet<Actor>();

				for (var i = -info.SquadSize / 2; i <= info.SquadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (info.SquadSize & 1) == 0)
						continue;

					// Includes the 90 degree rotation between body and world coordinates
					var so = info.SquadOffset;
					var spawnOffset = new WVec(i * so.Y, -Math.Abs(i) * so.X, 0).Rotate(attackRotation);

					var a = w.CreateActor(info.UnitType, new TypeDictionary
					{
						new CenterPositionInit(startPos + spawnOffset),
						new OwnerInit(self.Owner),
						new FacingInit(attackFacing),
					});

					var plane = a.Trait<Aircraft>().IsPlane;
					delta = new WVec(WDist.Zero, info.BeaconDistanceOffset, WDist.Zero).Rotate(attackRotation);

					if (plane)
					{
						if (info.Mission == AirstrikeMission.Attack)
						{
							a.QueueActivity(new FlyAttack(a, Target.FromPos(target + spawnOffset)));
						}
						else
						{
							a.QueueActivity(new Fly(a, Target.FromPos(target + spawnOffset)));
							a.QueueActivity(new AttackMoveActivity(a, new FlyCircleTimed(a, info.GuardDuration)));
						}

						a.QueueActivity(new FlyOffMap(a));
					}
					else
					{
						if (info.Mission == AirstrikeMission.Attack)
						{
							a.QueueActivity(new HeliAttack(a, Target.FromPos(target + spawnOffset)));
						}
						else
						{
							a.QueueActivity(new HeliFly(a, Target.FromPos(target + spawnOffset)));
							a.QueueActivity(new AttackMoveActivity(a, new HeliFlyCircleTimed(a, info.GuardDuration)));
						}

						var finishPos = target + (self.World.Map.DistanceToEdge(target, delta) + info.Cordon).Length * delta / 1024;
						a.QueueActivity(new HeliFly(a, Target.FromPos(finishPos + spawnOffset)));
					}

					a.QueueActivity(new RemoveSelf());

					aircrafts.Add(a);
				};

				var effect = new AirstrikePowerASEffect(self.World, self.Owner, target, aircrafts, info);
				self.World.Add(effect);
			});
		}
	}
}
