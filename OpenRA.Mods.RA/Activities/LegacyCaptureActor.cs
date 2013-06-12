﻿#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Activities
{
	class LegacyCaptureActor : Activity
	{
		Target target;

		public LegacyCaptureActor(Target target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled)
				return NextActivity;
			if (!target.IsValid)
				return NextActivity;

			var b = target.Actor.TraitOrDefault<Building>();
			if (b != null && b.Locked)
				return NextActivity;

			var capturesInfo = self.Info.Traits.Get<LegacyCapturesInfo>();
			var capturableInfo = target.Actor.Info.Traits.Get<LegacyCapturableInfo>();

			var health = target.Actor.Trait<Health>();
			var lowEnoughHealth = health.HP <= capturableInfo.CaptureThreshold * health.MaxHP;

			self.World.AddFrameEndTask(w =>
			{
				if (!capturesInfo.Sabotage || lowEnoughHealth || target.Actor.Owner.NonCombatant)
				{
					var oldOwner = target.Actor.Owner;

					target.Actor.ChangeOwner(self.Owner);

					foreach (var t in self.TraitsImplementing<INotifyCapture>())
						t.OnCapture(target.Actor, self, oldOwner, self.Owner);

					foreach (var t in self.World.ActorsWithTrait<INotifyOtherCaptured>())
						t.Trait.OnActorCaptured(t.Actor, target.Actor, self, oldOwner, self.Owner);

					if (b != null && b.Locked)
						b.Unlock();
				}
				else
				{
					int damage = (int)(health.MaxHP * capturesInfo.SabotageHPRemoval);
					target.Actor.InflictDamage(self, damage, null);
				}

				self.Destroy();
			});

			return this;
		}
	}
}
