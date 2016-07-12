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

using OpenRA.GameRules;
using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc.Traits
{
	class IonCannonPowerInfo : SupportPowerInfo, IRulesetLoaded
	{
		[ActorReference]
		[Desc("Actor to spawn when the attack starts")]
		public readonly string CameraActor = null;

		[Desc("Amount of time to keep the camera alive")]
		public readonly int CameraRemoveDelay = 25;

		[Desc("Effect sequence sprite image")]
		public readonly string Effect = "ionsfx";

		[Desc("Effect sequence to display")]
		[SequenceReference("Effect")] public readonly string EffectSequence = "idle";

		[PaletteReference] public readonly string EffectPalette = "effect";

		[Desc("Which weapon to fire"), WeaponReference]
		public readonly string Weapon = "IonCannon";

		public WeaponInfo WeaponInfo { get; private set; }

		[Desc("Apply the weapon impact this many ticks into the effect")]
		public readonly int WeaponDelay = 7;

		public override object Create(ActorInitializer init) { return new IonCannonPower(init.Self, this); }
		public void RulesetLoaded(Ruleset rules, ActorInfo ai) { WeaponInfo = rules.Weapons[Weapon.ToLowerInvariant()]; }
	}

	class IonCannonPower : SupportPower
	{
		readonly IonCannonPowerInfo info;

		public IonCannonPower(Actor self, IonCannonPowerInfo info)
			: base(self, info)
		{
			this.info = info;
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			self.World.AddFrameEndTask(w =>
			{
				Game.Sound.Play(Info.LaunchSound, self.World.Map.CenterOfCell(order.TargetLocation));
				w.Add(new IonCannon(self.Owner, info.WeaponInfo, w, order.TargetLocation, info.Effect, info.EffectSequence, info.EffectPalette, info.WeaponDelay));

				if (info.CameraActor == null)
					return;

				var camera = w.CreateActor(info.CameraActor, new TypeDictionary
				{
					new LocationInit(order.TargetLocation),
					new OwnerInit(self.Owner),
				});

				camera.QueueActivity(new Wait(info.CameraRemoveDelay));
				camera.QueueActivity(new RemoveSelf());
			});
		}
	}
}
