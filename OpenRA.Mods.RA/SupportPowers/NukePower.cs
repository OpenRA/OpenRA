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

		public override IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectTarget(order, manager, this);
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

			if (beacon != null)
			{
				Action removeBeacon = () => self.World.AddFrameEndTask(w =>
				{
					w.Remove(beacon);
					beacon = null;
				});

				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(npi.FlightDelay - npi.BeaconRemoveAdvance, removeBeacon)));
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
				var xy = wr.Position(wr.Viewport.ViewToWorldPx(Viewport.LastMousePos)).ToCPos();
				var radii = new HashSet<int>() { 0 };
				var steps = new int[Combat.falloff.Length - 1];
				for (int j = 1; j < Combat.falloff.Length; j++)
					steps[j - 1] = j;
				foreach (var wh in Rules.Weapons[(power.Info as NukePowerInfo).MissileWeapon.ToLowerInvariant()].Warheads)
				{
					//wh.Size.Do(Size => radii.Add(Size * 1024));
					steps.Do(j => radii.Add(j * wh.Spread.Range));
				}
				var damage = new Dictionary<int, double>();
				radii.Do(radius => damage[radius] = 0);
				int size = 0;
				foreach (var wh in Rules.Weapons[(power.Info as NukePowerInfo).MissileWeapon.ToLowerInvariant()].Warheads)
				{
					size = Math.Max(wh.Size[0], size);
					//radii.Where(radius => radius <= wh.Spread.Range).Do(radius => damage[radius] += wh.Damage);
					steps.Do(j => radii.Where(radius => ((j - 1) * wh.Spread.Range <= radius) && (radius < j * wh.Spread.Range))
						.Do(radius => damage[radius] += Convert.ToDouble(wh.Damage) / wh.Spread.Range * (Combat.falloff[j] * (radius - (j - 1) * wh.Spread.Range) + Combat.falloff[j - 1] * (j * wh.Spread.Range - radius))));
				}
				double[] levels = { 0.5, 0.25, 0.125, 0.0625 };
				foreach (var level in levels)
				{
					var radiusUp = damage.Where(radius => radius.Value >= level * damage.Values.Max()).ToDictionary(radius => radius.Key).Keys.Max();
					var radiusDown = damage.Keys.Where(radius => radius > radiusUp).Min();
					var range = new WRange(Convert.ToInt32((level * damage.Values.Max() * (radiusDown - radiusUp) - damage[radiusUp] * radiusDown + damage[radiusDown] * radiusUp) / (damage[radiusDown] - damage[radiusUp])));
					wr.DrawRangeCircleWithContrast(
						xy.CenterPosition,
						range,
						System.Drawing.Color.FromArgb(128, System.Drawing.Color.Red),
						System.Drawing.Color.FromArgb(96, System.Drawing.Color.Black)
					);
				}
				wr.DrawRangeCircleWithContrast(
					xy.CenterPosition,
					WRange.FromCells(size),
					System.Drawing.Color.FromArgb(128, System.Drawing.Color.Yellow),
					System.Drawing.Color.FromArgb(96, System.Drawing.Color.Yellow)
				);
				/*int range1 = 0;
				int range2 = 0;
				foreach (var wh in Rules.Weapons[(power.Info as NukePowerInfo).MissileWeapon.ToLowerInvariant()].Warheads)
				{
					range1 = Math.Max(wh.Size[0], range1);
					wr.DrawRangeCircleWithContrast(
						xy.CenterPosition,
						WRange.FromCells(wh.Size[0]),
						System.Drawing.Color.FromArgb(128, System.Drawing.Color.Red),
						System.Drawing.Color.FromArgb(96, System.Drawing.Color.Black)
					);
					int damage = wh.Damage;
					int i = Array.FindLastIndex(Combat.falloff, f => f * damage >= 1);
					int x=0;
					if (i >= Combat.falloff.Length - 1)
						x = wh.Spread.Range * 7;
					else if (i != -1)
						x = Convert.ToInt32((1f / damage + i * Combat.falloff[i + 1] - (i + 1) * Combat.falloff[i]) / (Combat.falloff[i + 1] - Combat.falloff[i]) * wh.Spread.Range);
					range2 = Math.Max(x, range2);
				}
				wr.DrawRangeCircleWithContrast(
					xy.CenterPosition,
					new WRange(range2),
					System.Drawing.Color.FromArgb(30, System.Drawing.Color.Red),
					System.Drawing.Color.FromArgb(30, System.Drawing.Color.Black)
				);*/
			}
			public string GetCursor(World world, CPos xy, MouseInput mi) { return world.Map.IsInMap(xy) ? cursor : "generic-blocked"; }
		}
	}
}
