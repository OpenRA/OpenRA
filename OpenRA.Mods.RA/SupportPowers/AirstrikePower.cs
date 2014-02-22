#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.FileFormats;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Air;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AirstrikePowerInfo : SupportPowerInfo
	{
		[ActorReference]
		public readonly string UnitType = "badr.bomber";
		public readonly int SquadSize = 1;
		public readonly WVec SquadOffset = new WVec(-1536, 1536, 0);

		public readonly int QuantizedFacings = 32;
		public readonly WDist Cordon = new WDist(5120);

		[ActorReference]
		public readonly string FlareType = null;
		public readonly int FlareTime = 3000; // 2 minutes

		public override object Create(ActorInitializer init) { return new AirstrikePower(init.self, this); }
	}

	class AirstrikePower : SupportPower
	{
		public AirstrikePower(Actor self, AirstrikePowerInfo info)
			: base(self, info) { }

		public override void Activate(Actor self, Order order)
		{
			var info = Info as AirstrikePowerInfo;

			var attackFacing = Util.QuantizeFacing(self.World.SharedRandom.Next(256), info.QuantizedFacings) * (256 / info.QuantizedFacings);
			var attackRotation = WRot.FromFacing(attackFacing);
			var delta = new WVec(0, -1024, 0).Rotate(attackRotation);

			var altitude = Rules.Info[info.UnitType].Traits.Get<PlaneInfo>().CruiseAltitude.Range;
			var target = order.TargetLocation.CenterPosition + new WVec(0, 0, altitude);
			var startEdge = target - (self.World.DistanceToMapEdge(target, -delta) + info.Cordon).Range * delta / 1024;
			var finishEdge = target + (self.World.DistanceToMapEdge(target, delta) + info.Cordon).Range * delta / 1024;

			self.World.AddFrameEndTask(w =>
			{
				var notification = self.Owner.IsAlliedWith(self.World.RenderPlayer) ? Info.LaunchSound : Info.IncomingSound;
				Sound.Play(notification);

				Actor flare = null;
				if (info.FlareType != null)
				{
					flare = w.CreateActor(info.FlareType, new TypeDictionary
					{
						new LocationInit(order.TargetLocation),
						new OwnerInit(self.Owner),
					});

					flare.QueueActivity(new Wait(info.FlareTime));
					flare.QueueActivity(new RemoveSelf());
				}

				for (var i = -info.SquadSize / 2; i <= info.SquadSize / 2; i++)
				{
					// Even-sized squads skip the lead plane
					if (i == 0 && (info.SquadSize & 1) == 0)
						continue;

					// Includes the 90 degree rotation between body and world coordinates
					var so = info.SquadOffset;
					var spawnOffset = new WVec(i*so.Y, -Math.Abs(i)*so.X, 0).Rotate(attackRotation);
					var targetOffset = new WVec(i*so.Y, 0, 0).Rotate(attackRotation);

					var a = w.CreateActor(info.UnitType, new TypeDictionary
					{
						new CenterPositionInit(startEdge + spawnOffset),
						new OwnerInit(self.Owner),
						new FacingInit(attackFacing),
					});

					a.Trait<AttackBomber>().SetTarget(target + targetOffset);

					if (flare != null)
						a.QueueActivity(new CallFunc(() => flare.Destroy()));

					a.QueueActivity(new Fly(a, Target.FromPos(finishEdge + spawnOffset)));
					a.QueueActivity(new RemoveSelf());
				}
			});
		}
	}
}
