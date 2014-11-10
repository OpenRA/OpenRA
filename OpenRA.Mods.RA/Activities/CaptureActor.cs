#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Activities
{
	class CaptureActor : Enter
	{
		readonly Actor actor;
		readonly Capturable capturable;
		readonly CapturesInfo capturesInfo;

		public CaptureActor(Actor self, Actor target)
			: base(self, target)
		{
			actor = target;
			capturesInfo = self.Info.Traits.Get<CapturesInfo>();
			capturable = target.Trait<Capturable>();
		}

		protected override bool CanReserve(Actor self)
		{
			return !capturable.BeingCaptured && capturable.Info.CanBeTargetedBy(self, actor.Owner);
		}

		protected override void OnInside(Actor self)
		{
			if (actor.Flagged(ActorFlag.Dead) || capturable.BeingCaptured)
				return;

			var b = actor.TraitOrDefault<Building>();
			if (b != null && !b.Lock())
				return;

			self.World.AddFrameEndTask(w =>
			{
				if (b != null && b.Locked)
					b.Unlock();

				if (actor.Flagged(ActorFlag.Dead) || capturable.BeingCaptured)
					return;

				var health = actor.Trait<Health>();

				var lowEnoughHealth = health.HP <= capturable.Info.CaptureThreshold * health.MaxHP;
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
		}
	}
}
