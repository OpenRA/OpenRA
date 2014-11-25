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
using OpenRA.Effects;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Effects;
using OpenRA.Mods.RA.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class NukePowerInfo : SupportPowerInfo, Requires<IBodyOrientationInfo>
	{
		[WeaponReference]
		public readonly string MissileWeapon = "";
		public readonly WVec SpawnOffset = WVec.Zero;

		[Desc("Travel time - split equally between ascent and descent")]
		public readonly int FlightDelay = 400;

		[Desc("Visual ascent velocity in WRange / tick")]
		public readonly WRange FlightVelocity = new WRange(512);

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

		public override object Create(ActorInitializer init) { return new NukePower(init.self, this); }
	}

	class NukePower : SupportPower
	{
		IBodyOrientation body;

		public NukePower(Actor self, NukePowerInfo info)
			: base(self, info)
		{
			body = self.Trait<IBodyOrientation>();
		}

		public override IOrderGenerator OrderGenerator(OrderCode order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectGenericPowerTarget(order, manager, "nuke", MouseButton.Left);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				Sound.Play(Info.LaunchSound);
			else
				Sound.Play(Info.IncomingSound);

			var npi = Info as NukePowerInfo;
			var rb = self.Trait<RenderSimple>();
			rb.PlayCustomAnim(self, "active");

			var targetPosition = self.World.Map.CenterOfCell(order.TargetLocation);
			var missile = new NukeLaunch(self.Owner, npi.MissileWeapon,
				self.CenterPosition + body.LocalToWorld(npi.SpawnOffset),
				targetPosition,
				npi.FlightVelocity, npi.FlightDelay, npi.SkipAscent);

			self.World.AddFrameEndTask(w => w.Add(missile));

			if (npi.CameraActor != null)
			{
				var camera = self.World.CreateActor(false, npi.CameraActor, new TypeDictionary
				{
					new LocationInit(order.TargetLocation),
					new OwnerInit(self.Owner),
				});

				camera.QueueActivity(new Wait(npi.CameraSpawnAdvance + npi.CameraRemoveDelay));
				camera.QueueActivity(new RemoveSelf());

				Action addCamera = () => self.World.AddFrameEndTask(w => w.Add(camera));
				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(npi.FlightDelay - npi.CameraSpawnAdvance, addCamera)));
			}

			if (Info.DisplayBeacon)
			{
				var beacon = new Beacon(
					order.Player,
					targetPosition,
					Info.BeaconPalettePrefix,
					Info.BeaconPoster,
					Info.BeaconPosterPalette,
					() => missile.FractionComplete
				);


				Action removeBeacon = () => self.World.AddFrameEndTask(w =>
				{
					w.Remove(beacon);
					beacon = null;
				});

				self.World.AddFrameEndTask(w =>
				{
					w.Add(beacon);
					w.Add(new DelayedAction(npi.FlightDelay - npi.BeaconRemoveAdvance, removeBeacon));
				});
			}
		}
	}
}
