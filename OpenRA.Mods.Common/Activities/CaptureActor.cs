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

using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class CaptureActor : Enter
	{
		readonly Actor actor;
		readonly Building building;
		readonly Capturable capturable;
		readonly CapturesInfo capturesInfo;
		readonly Health health;

		public CaptureActor(Actor self, Actor target)
			: base(self, target, EnterBehaviour.Dispose)
		{
			actor = target;
			building = actor.TraitOrDefault<Building>();
			capturesInfo = self.Info.TraitInfo<CapturesInfo>();
			capturable = target.Trait<Capturable>();
			health = actor.Trait<Health>();
		}

		protected override bool CanReserve(Actor self)
		{
			return !capturable.BeingCaptured && capturable.Info.CanBeTargetedBy(self, actor.Owner);
		}

		protected override void OnInside(Actor self)
		{
			if (actor.IsDead || capturable.BeingCaptured)
				return;

			if (building != null && !building.Lock())
				return;

			self.World.AddFrameEndTask(w =>
			{
				if (building != null && building.Locked)
					building.Unlock();

				if (actor.IsDead || capturable.BeingCaptured)
					return;

				var lowEnoughHealth = health.HP <= capturable.Info.CaptureThreshold * health.MaxHP / 100;
				if (!capturesInfo.Sabotage || lowEnoughHealth || actor.Owner.NonCombatant)
				{
					var oldOwner = actor.Owner;

					actor.ChangeOwner(self.Owner);

					foreach (var t in actor.TraitsImplementing<INotifyCapture>())
						t.OnCapture(actor, self, oldOwner, self.Owner);

					if (building != null && building.Locked)
						building.Unlock();

					if (self.Owner.Stances[oldOwner].HasStance(capturesInfo.PlayerExperienceStances))
					{
						var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
						if (exp != null)
							exp.GiveExperience(capturesInfo.PlayerExperience);
					}
				}
				else
				{
					var damage = health.MaxHP * capturesInfo.SabotageHPRemoval / 100;
					actor.InflictDamage(self, new Damage(damage));
				}

				self.Dispose();
			});
		}
	}
}
