#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class CaptureActor : Enter
	{
		readonly Actor actor;
		readonly Building building;
		readonly CaptureManager targetManager;
		readonly CaptureManager manager;

		public CaptureActor(Actor self, Actor target)
			: base(self, target, EnterBehaviour.Dispose)
		{
			actor = target;
			building = actor.TraitOrDefault<Building>();
			manager = self.Trait<CaptureManager>();
			targetManager = target.Trait<CaptureManager>();
		}

		protected override bool CanReserve(Actor self)
		{
			return !actor.IsDead && !targetManager.BeingCaptured && targetManager.CanBeTargetedBy(actor, self, manager);
		}

		protected override void OnInside(Actor self)
		{
			if (!CanReserve(self))
				return;

			if (building != null && !building.Lock())
				return;

			self.World.AddFrameEndTask(w =>
			{
				if (building != null && building.Locked)
					building.Unlock();

				// Prioritize capturing over sabotaging
				var captures = manager.ValidCapturesWithLowestSabotageThreshold(self, actor, targetManager);
				if (captures == null)
					return;

				// Sabotage instead of capture
				if (captures.Info.SabotageThreshold > 0 && !actor.Owner.NonCombatant)
				{
					var health = actor.Trait<IHealth>();

					// Cast to long to avoid overflow when multiplying by the health
					if (100 * (long)health.HP > captures.Info.SabotageThreshold * (long)health.MaxHP)
					{
						var damage = (int)((long)health.MaxHP * captures.Info.SabotageHPRemoval / 100);
						actor.InflictDamage(self, new Damage(damage));

						self.Dispose();
						return;
					}
				}

				// Do the capture
				var oldOwner = actor.Owner;

				actor.ChangeOwner(self.Owner);

				foreach (var t in actor.TraitsImplementing<INotifyCapture>())
					t.OnCapture(actor, self, oldOwner, self.Owner);

				if (building != null && building.Locked)
					building.Unlock();

				if (self.Owner.Stances[oldOwner].HasStance(captures.Info.PlayerExperienceStances))
				{
					var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
					if (exp != null)
						exp.GiveExperience(captures.Info.PlayerExperience);
				}

				self.Dispose();
			});
		}

		public override Activity Tick(Actor self)
		{
			if (!targetManager.CanBeTargetedBy(actor, self, manager))
				Cancel(self);

			return base.Tick(self);
		}
	}
}
