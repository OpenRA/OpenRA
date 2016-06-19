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
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class NukePowerInfo : SupportPowerInfo, IRulesetLoaded, Requires<BodyOrientationInfo>
	{
		[WeaponReference, FieldLoader.Require]
		[Desc("Weapon to use for the impact.",
			"Also image to use for the missile.")]
		public readonly string MissileWeapon = "";

		[Desc("Sprite sequence for the ascending missile.")]
		[SequenceReference("MissileWeapon")] public readonly string MissileUp = "up";

		[Desc("Sprite sequence for the descending missile.")]
		[SequenceReference("MissileWeapon")] public readonly string MissileDown = "down";

		[Desc("Offset from the actor the missile spawns on.")]
		public readonly WVec SpawnOffset = WVec.Zero;

		[Desc("Palette to use for the missile weapon image.")]
		[PaletteReference("IsPlayerPalette")] public readonly string MissilePalette = "effect";

		[Desc("Custom palette is a player palette BaseName")]
		public readonly bool IsPlayerPalette = false;

		[Desc("Travel time - split equally between ascent and descent")]
		public readonly int FlightDelay = 400;

		[Desc("Visual ascent velocity in WDist / tick")]
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

		[Desc("Corresponds to `Type` from `FlashPaletteEffect` on the world actor.")]
		public readonly string FlashType = null;

		[SequenceReference]
		[Desc("Sequence the launching actor should play when activating this power.")]
		public readonly string ActivationSequence = "active";

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new NukePower(init.Self, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai) { WeaponInfo = rules.Weapons[MissileWeapon.ToLowerInvariant()]; }
	}

	class NukePower : SupportPower
	{
		readonly NukePowerInfo info;
		readonly BodyOrientation body;

		public NukePower(Actor self, NukePowerInfo info)
			: base(self, info)
		{
			body = self.Trait<BodyOrientation>();
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				Game.Sound.Play(Info.LaunchSound);
			else
			{
				Game.Sound.Play(Info.IncomingSound);
				Game.Sound.PlayNotification(self.World.Map.Rules, self.Owner, "Speech",
					Info.IncomingSpeechNotification, self.Owner.Faction.InternalName);
			}

			if (!string.IsNullOrEmpty(info.ActivationSequence))
			{
				var wsb = self.Trait<WithSpriteBody>();
				wsb.PlayCustomAnimation(self, info.ActivationSequence, () => wsb.CancelCustomAnimation(self));
			}

			var targetPosition = self.World.Map.CenterOfCell(order.TargetLocation);
			var palette = info.IsPlayerPalette ? info.MissilePalette + self.Owner.InternalName : info.MissilePalette;
			var missile = new NukeLaunch(self.Owner, info.MissileWeapon, info.WeaponInfo, palette, info.MissileUp, info.MissileDown,
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
					Info.BeaconPaletteIsPlayerPalette,
					Info.BeaconPalette,
					Info.BeaconImage,
					Info.BeaconPoster,
					Info.BeaconPosterPalette,
					Info.ArrowSequence,
					Info.CircleSequence,
					Info.ClockSequence,
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
