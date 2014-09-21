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
	class CaptureActor : Activity
	{
		Target target;

		public CaptureActor(Target target) { this.target = target; }

		public override Activity Tick(Actor self)
		{
			if (IsCanceled || !target.IsValidFor(self))
				return NextActivity;

			if (target.Type != TargetType.Actor)
				return NextActivity;

			var actor = target.Actor;
			var b = actor.TraitOrDefault<Building>();
			if (b != null && b.Locked)
				return NextActivity;

			var capturesInfo = self.Info.Traits.Get<CapturesInfo>();
			var capturableInfo = actor.Info.Traits.Get<CapturableInfo>();

			var health = actor.Trait<Health>();

			self.World.AddFrameEndTask(w =>
			{
				if (actor.IsDead())
					return;

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
