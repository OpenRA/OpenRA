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
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class LegacyCaptureActor : Activity
	{
		Target target;

		public LegacyCaptureActor(Target target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || target.Type != TargetType.Actor)
				return NextActivity;

			var actor = target.Actor;
			var b = actor.TraitOrDefault<Building>();
			if (b != null && b.Locked)
				return NextActivity;

			var capturesInfo = self.Info.Traits.Get<LegacyCapturesInfo>();
			var capturableInfo = actor.Info.Traits.Get<LegacyCapturableInfo>();

			var health = actor.Trait<Health>();

			self.World.AddFrameEndTask(w =>
			{
				var lowEnoughHealth = health.HP <= capturableInfo.CaptureThreshold * health.MaxHP;
				if (!capturesInfo.Sabotage || lowEnoughHealth || actor.Owner.NonCombatant)
				{
					var oldOwner = actor.Owner;

					actor.ChangeOwner(self.Owner);

					foreach (var t in actor.TraitsImplementing<INotifyCapture>())
						t.OnCapture(actor, self, oldOwner, self.Owner);

					if (b != null && b.Locked)
						b.Unlock();
				}
				else
				{
					var damage = (int)(health.MaxHP * capturesInfo.SabotageHPRemoval);
					actor.InflictDamage(self, damage, null);
				}

				self.Destroy();
			});

			return this;
		}
	}
}
