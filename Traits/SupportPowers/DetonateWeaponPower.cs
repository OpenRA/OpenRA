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
using System.Linq;
using OpenRA.Effects;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Effects;
using OpenRA.Mods.Common.Traits;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("Support power for detonating a weapon at the target position.")]
	class DetonateWeaponPowerInfo : SupportPowerInfo, IRulesetLoaded
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

		public WeaponInfo WeaponInfo { get; private set; }

		public override object Create(ActorInitializer init) { return new DetonateWeaponPower(init.Self, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai) { WeaponInfo = rules.Weapons[Weapon.ToLowerInvariant()]; }
	}

	class DetonateWeaponPower : SupportPower, ITick
	{
		readonly DetonateWeaponPowerInfo info;
		int ticks;

		public DetonateWeaponPower(Actor self, DetonateWeaponPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			if (self.Owner.IsAlliedWith(self.World.RenderPlayer))
				Game.Sound.Play(Info.LaunchSound);
			else
				Game.Sound.Play(Info.IncomingSound);

			if (!string.IsNullOrEmpty(info.ActivationSequence))
			{
				var wsb = self.Trait<WithSpriteBody>();
				wsb.PlayCustomAnimation(self, info.ActivationSequence);
			}

			var targetPosition = self.World.Map.CenterOfCell(order.TargetLocation);

			Action detonateWeapon = () => self.World.AddFrameEndTask(w => {
				info.WeaponInfo.Impact(Target.FromCell(w, order.TargetLocation), self, Enumerable.Empty<int>());
			});
			self.World.AddFrameEndTask(w => w.Add(new DelayedAction(info.ActivationDelay, detonateWeapon)));

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
				self.World.AddFrameEndTask(w => w.Add(new DelayedAction(info.ActivationDelay - info.CameraSpawnAdvance, addCamera)));
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
						w.Add(new DelayedAction(info.ActivationDelay - info.BeaconRemoveAdvance, removeBeacon));
					});
			}
		}

		public void Tick(Actor self)
		{
			ticks++;
		}

		float FractionComplete { get { return ticks * 1f / info.ActivationDelay; } }
	}
}
