#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Effects;
using OpenRA.Mods.RA.Activities;
using OpenRA.Mods.RA.Effects;
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

		public readonly string DisplayRing = "0";

		public override object Create(ActorInitializer init)
		{
			double damage;
			if (DisplayRing == "Smudge")
				RingRangeP = Combat.FindSmudgeRange(init.self.World.Map.Rules.Weapons[MissileWeapon.ToLowerInvariant()].Warheads);
			else if (!WRange.TryParse(DisplayRing, out RingRangeP))
				if (DisplayRing.Contains('%') && double.TryParse(DisplayRing.TrimEnd(new char[] { '%' }), out damage))
					RingRangeP = Combat.FindDamageRange(init.self.World.Map.Rules.Weapons[MissileWeapon.ToLowerInvariant()].Warheads, damage / 100);
			return new NukePower(init.self, this);
		}
	}

	class NukePower : SupportPower
	{
		IBodyOrientation body;

		public NukePower(Actor self, NukePowerInfo info)
			: base(self, info)
		{
			body = self.Trait<IBodyOrientation>();
		}

		public override IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectTarget(order, manager, this);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			var npi = Info as NukePowerInfo;
			if (Info.DisplayBeacon)
			{
				beacon = new Beacon(
					order.Player,
					order.TargetLocation.CenterPosition,
					Info.BeaconDuration == -1 ? npi.FlightDelay - npi.BeaconRemoveAdvance : Info.BeaconDuration,
					Info.BeaconPalettePrefix,
					Info.BeaconPoster,
					Info.BeaconPosterPalette);

				self.World.Add(beacon);
			}

			if (Info.DisplayRadarPing && manager.RadarPings != null)
				manager.RadarPings.Value.Add(
					() => order.Player.IsAlliedWith(self.World.RenderPlayer),
					order.TargetLocation.CenterPosition,
					order.Player.Color.RGB,
					Info.BeaconDuration == -1 ? npi.FlightDelay - npi.BeaconRemoveAdvance : Info.BeaconDuration);

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				Sound.Play(Info.LaunchSound);
			else
				Sound.Play(Info.IncomingSound);

			var rb = self.Trait<RenderSimple>();
			rb.PlayCustomAnim(self, "active");

			self.World.AddFrameEndTask(w => w.Add(new NukeLaunch(self.Owner, npi.MissileWeapon,
				self.CenterPosition + body.LocalToWorld(npi.SpawnOffset),
				order.TargetLocation.CenterPosition,
				npi.FlightVelocity, npi.FlightDelay, npi.SkipAscent)));

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
		}

		class SelectTarget : IOrderGenerator
		{
			readonly NukePower power;
			readonly SupportPowerManager manager;
			readonly string order;
			readonly string cursor;
			readonly MouseButton expectedButton;

			public SelectTarget(string order, SupportPowerManager manager, NukePower power)
			{
				this.manager = manager;
				this.order = order;
				this.power = power;
				this.cursor = "nuke";
				expectedButton = MouseButton.Left;
			}

			public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
			{
				world.CancelInputMode();
				if (mi.Button == expectedButton && world.Map.IsInMap(xy))
					yield return new Order(order, manager.self, false) { TargetLocation = xy };
			}

			public virtual void Tick(World world)
			{
				// Cancel the OG if we can't use the power
				if (!manager.Powers.ContainsKey(order))
					world.CancelInputMode();
			}

			public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
			public void RenderAfterWorld(WorldRenderer wr, World world)
			{
				if (power.Info.RingRange.Range > 0)
					wr.DrawRangeCircleWithContrast(
						wr.Position(wr.Viewport.ViewToWorldPx(Viewport.LastMousePos)).ToCPos().CenterPosition,
						power.Info.RingRange,
						System.Drawing.Color.FromArgb(128, System.Drawing.Color.Red),
						System.Drawing.Color.FromArgb(96, System.Drawing.Color.Black)
					);
			}
			public string GetCursor(World world, CPos xy, MouseInput mi) { return world.Map.IsInMap(xy) ? cursor : "generic-blocked"; }
		}
	}
}
