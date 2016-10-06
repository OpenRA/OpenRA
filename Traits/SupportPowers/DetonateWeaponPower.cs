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
using System.Drawing;
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Support power for detonating a weapon at the target position.")]
	public class DetonateWeaponPowerInfo : SupportPowerInfo, IRulesetLoaded
	{
		[WeaponReference]
		public readonly string Weapon = "";

		[Desc("Delay between activation and explosion")]
		public readonly int ActivationDelay = 10;

		[Desc("Amount of time before detonation to remove the beacon")]
		public readonly int BeaconRemoveAdvance = 5;

		[ActorReference]
		[Desc("Actor to spawn before detonation")]
		public readonly string CameraActor = null;

		[Desc("Amount of time before detonation to spawn the camera")]
		public readonly int CameraSpawnAdvance = 5;

		[Desc("Amount of time after detonation to remove the camera")]
		public readonly int CameraRemoveDelay = 5;

		[SequenceReference]
		[Desc("Sequence the launching actor should play when activating this power.")]
		public readonly string ActivationSequence = "active";

		[Desc("Altitude above terrain below which to explode. Zero effectively deactivates airburst.")]
		public readonly WDist AirburstAltitude = WDist.Zero;

		public readonly WDist TargetCircleRange = WDist.Zero;
		public readonly Color TargetCircleColor = Color.White;
		public readonly bool TargetCircleUsePlayerColor = false;

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new DetonateWeaponPower(init.Self, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai) { WeaponInfo = rules.Weapons[Weapon.ToLowerInvariant()]; }
	}

	public class DetonateWeaponPower : SupportPower, ITick
	{
		public new readonly DetonateWeaponPowerInfo Info;
		int ticks;

		public DetonateWeaponPower(Actor self, DetonateWeaponPowerInfo info)
			: base(self, info)
		{
			this.Info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				Game.Sound.Play(Info.LaunchSound);
			else
				Game.Sound.Play(Info.IncomingSound);

			if (!string.IsNullOrEmpty(Info.ActivationSequence))
			{
				var wsb = self.Trait<WithSpriteBody>();
				wsb.PlayCustomAnimation(self, Info.ActivationSequence);
			}

			var targetPosition = self.World.Map.CenterOfCell(order.TargetLocation) + new WVec(WDist.Zero, WDist.Zero, Info.AirburstAltitude);

			Action detonateWeapon = () => self.World.AddFrameEndTask(w => Info.WeaponInfo.Impact(Target.FromPos(targetPosition), self, Enumerable.Empty<int>()));

			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(Info.ActivationDelay, detonateWeapon)));

			if (Info.CameraActor != null)
			{
				var camera = self.World.CreateActor(false, Info.CameraActor, new TypeDictionary
					{
						new LocationInit(order.TargetLocation),
						new OwnerInit(self.Owner),
					});

				camera.QueueActivity(new Wait(Info.CameraSpawnAdvance + Info.CameraRemoveDelay));
				camera.QueueActivity(new RemoveSelf());

				Action addCamera = () => self.World.AddFrameEndTask(w => w.Add(camera));
				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(Info.ActivationDelay - Info.CameraSpawnAdvance, addCamera)));
			}

			if (Info.DisplayBeacon)
			{
				var beacon = new Beacon(
					order.Player,
					targetPosition,
					Info.BeaconPaletteIsPlayerPalette,
					Info.BeaconPalette,
					Info.BeaconImage,
					Info.BeaconPoster,
					Info.BeaconPosterPalette,
					Info.ArrowSequence,
					Info.CircleSequence,
					Info.ClockSequence,
					() => FractionComplete);

				Action removeBeacon = () => self.World.AddFrameEndTask(w =>
					{
						w.Remove(beacon);
						beacon = null;
					});

				self.World.AddFrameEndTask(w =>
					{
						w.Add(beacon);
						w.Add(new DelayedAction(Info.ActivationDelay - Info.BeaconRemoveAdvance, removeBeacon));
					});
			}
		}

		public void Tick(Actor self)
		{
			ticks++;
		}

		public override void SelectTarget(Actor self, string order, SupportPowerManager manager)
		{
			Game.Sound.PlayToPlayer(manager.Self.Owner, Info.SelectTargetSound);
			self.World.OrderGenerator = new SelectDetonateWeaponPowerTarget(order, manager, this);
		}

		float FractionComplete { get { return ticks * 1f / Info.ActivationDelay; } }
	}

	public class SelectDetonateWeaponPowerTarget : IOrderGenerator
	{
		readonly SupportPowerManager manager;
		readonly string order;
		readonly DetonateWeaponPower power;

		public SelectDetonateWeaponPowerTarget(string order, SupportPowerManager manager, DetonateWeaponPower power)
		{
			// Clear selection if using Left-Click Orders
			if (Game.Settings.Game.UseClassicMouseStyle)
				manager.Self.World.Selection.Clear();

			this.manager = manager;
			this.order = order;
			this.power = power;
		}

		public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			world.CancelInputMode();
			if (mi.Button == MouseButton.Left && world.Map.Contains(cell))
				yield return new Order(order, manager.Self, false) { TargetLocation = cell, SuppressVisualFeedback = true };
		}

		public virtual void Tick(World world)
		{
			// Cancel the OG if we can't use the power
			if (!manager.Powers.ContainsKey(order))
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }

		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world)
		{
			var xy = wr.Viewport.ViewToWorld(Viewport.LastMousePos);

			if (power.Info.TargetCircleRange == WDist.Zero)
			{
				yield break;
			}
			else
			{
				yield return new RangeCircleRenderable(
					world.Map.CenterOfCell(xy),
					power.Info.TargetCircleRange,
					0,
					power.Info.TargetCircleUsePlayerColor ? power.Self.Owner.Color.RGB : power.Info.TargetCircleColor,
					Color.FromArgb(96, Color.Black));
			}
		}

		public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			return world.Map.Contains(cell) ? power.Info.Cursor : "generic-blocked";
		}
	}
}
