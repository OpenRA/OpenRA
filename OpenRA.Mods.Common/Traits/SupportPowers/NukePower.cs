#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Effects;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class NukePowerInfo : SupportPowerInfo, Requires<IBodyOrientationInfo>
	{
		[WeaponReference]
		public readonly string MissileWeapon = "";
		public readonly WVec SpawnOffset = WVec.Zero;

		[Desc("Travel time - split equally between ascent and descent")]
		public readonly int FlightDelay = 400;

		[Desc("Visual ascent velocity in WRange / tick")]
		public readonly WDist FlightVelocity = new WDist(512);

		[Desc("Descend immediately on the target, with half the FlightDelay")]
		public readonly bool SkipAscent = false;

		[Desc("Amount of time before detonation to remove the beacon")]
		public readonly int BeaconRemoveAdvance = 25;

		[ActorReference]
		[Desc("Actor to spawn before detonation")]
		public readonly string CameraActor = null;

		[Desc("Amount of time before detonation to spawn the camera")]
		public readonly int CameraSpawnAdvance = 25;

		[Desc("Amount of time after detonation to remove the camera")]
		public readonly int CameraRemoveDelay = 25;

		public readonly string FlashType = null;

		public override object Create(ActorInitializer init) { return new NukePower(init.Self, this); }
	}

	class NukePower : SupportPower
	{
		readonly NukePowerInfo info;
		IBodyOrientation body;

		public NukePower(Actor self, NukePowerInfo info)
			: base(self, info)
		{
			body = self.Trait<IBodyOrientation>();
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				Sound.Play(Info.LaunchSound);
			else
				Sound.Play(Info.IncomingSound);

			var rb = self.Trait<RenderSimple>();
			rb.PlayCustomAnim(self, "active");

			var targetPosition = self.World.Map.CenterOfCell(order.TargetLocation);
			var missile = new NukeLaunch(self.Owner, info.MissileWeapon,
				self.CenterPosition + body.LocalToWorld(info.SpawnOffset),
				targetPosition,
				info.FlightVelocity, info.FlightDelay, info.SkipAscent,
				info.FlashType);

			self.World.AddFrameEndTask(w => w.Add(missile));

			if (info.CameraActor != null)
			{
				var camera = self.World.CreateActor(false, info.CameraActor, new TypeDictionary
				{
					new LocationInit(order.TargetLocation),
					new OwnerInit(self.Owner),
				});

				camera.QueueActivity(new Wait(info.CameraSpawnAdvance + info.CameraRemoveDelay));
				camera.QueueActivity(new RemoveSelf());

				Action addCamera = () => self.World.AddFrameEndTask(w => w.Add(camera));
				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(info.FlightDelay - info.CameraSpawnAdvance, addCamera)));
			}

			if (Info.DisplayBeacon)
			{
				var beacon = new Beacon(
					order.Player,
					targetPosition,
					Info.BeaconPalettePrefix,
					Info.BeaconPoster,
					Info.BeaconPosterPalette,
					() => missile.FractionComplete);

				Action removeBeacon = () => self.World.AddFrameEndTask(w =>
				{
					w.Remove(beacon);
					beacon = null;
				});

				self.World.AddFrameEndTask(w =>
				{
					w.Add(beacon);
					w.Add(new DelayedAction(info.FlightDelay - info.BeaconRemoveAdvance, removeBeacon));
				});
			}
		}
	}
}
