#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.Cnc.Effects;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Activities;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class IonCannonPowerInfo : SupportPowerInfo
	{
		[ActorReference]
		[Desc("Actor to spawn when the attack starts")]
		public readonly string CameraActor = null;
		[Desc("Amount of time to keep the camera alive")]
		public readonly int CameraRemoveDelay = 25;
		[Desc("Effect sequence to display")]
		public readonly string Effect = "ionsfx";
		public readonly string EffectPalette = "effect";
		[Desc("Which weapon to fire")]
		public readonly string Weapon = "IonCannon";

		public override object Create(ActorInitializer init) { return new IonCannonPower(init.self, this); }
	}

	class IonCannonPower : SupportPower
	{
		public IonCannonPower(Actor self, IonCannonPowerInfo info) : base(self, info) { }

		public override IOrderGenerator OrderGenerator(string order, SupportPowerManager manager)
		{
			Sound.PlayToPlayer(manager.self.Owner, Info.SelectTargetSound);
			return new SelectGenericPowerTarget(order, manager, "ioncannon", MouseButton.Left);
		}

		public override void Activate(Actor self, Order order, SupportPowerManager manager)
		{
			base.Activate(self, order, manager);

			self.World.AddFrameEndTask(w =>
			{
				var info = Info as IonCannonPowerInfo;
				Sound.Play(Info.LaunchSound, order.TargetLocation.CenterPosition);
				w.Add(new IonCannon(self.Owner, info.Weapon, w, order.TargetLocation, info.Effect, info.EffectPalette));

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
