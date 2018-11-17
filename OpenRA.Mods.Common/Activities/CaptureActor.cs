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

using OpenRA.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Activities
{
	public class CaptureActor : Enter
	{
		readonly Actor actor;
		readonly CaptureManager targetManager;
		readonly CaptureManager manager;

		public CaptureActor(Actor self, Actor target)
			: base(self, target, EnterBehaviour.Exit)
		{
			actor = target;
			manager = self.Trait<CaptureManager>();
			targetManager = target.Trait<CaptureManager>();
		}

		protected override bool CanReserve(Actor self)
		{
			return !actor.IsDead && !targetManager.BeingCaptured && targetManager.CanBeTargetedBy(actor, self, manager);
		}

		protected override bool TryStartEnter(Actor self)
		{
			// StartCapture returns false when a capture delay is enabled
			// We wait until it returns true before allowing entering the target
			Captures captures;
			if (!manager.StartCapture(self, actor, targetManager, out captures))
				return false;

			if (!captures.Info.ConsumedByCapture)
			{
				// Immediately capture without entering or disposing the actor
				DoCapture(self, captures);
				AbortOrExit(self);
				return false;
			}

			return true;
		}

		protected override void OnInside(Actor self)
		{
			if (!CanReserve(self))
				return;

			// Prioritize capturing over sabotaging
			var captures = manager.ValidCapturesWithLowestSabotageThreshold(self, actor, targetManager);
			if (captures == null)
				return;

			DoCapture(self, captures);
		}

		void DoCapture(Actor self, Captures captures)
		{
			var oldOwner = actor.Owner;
			self.World.AddFrameEndTask(w =>
			{
				// The target died or was already captured during this tick
				if (actor.IsDead || oldOwner != actor.Owner)
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

						if (captures.Info.ConsumedByCapture)
							self.Dispose();

						return;
					}
				}

				// Do the capture
				actor.ChangeOwnerSync(self.Owner);

				foreach (var t in actor.TraitsImplementing<INotifyCapture>())
					t.OnCapture(actor, self, oldOwner, self.Owner, captures.Info.CaptureTypes);

				if (self.Owner.Stances[oldOwner].HasStance(captures.Info.PlayerExperienceStances))
				{
					var exp = self.Owner.PlayerActor.TraitOrDefault<PlayerExperience>();
					if (exp != null)
						exp.GiveExperience(captures.Info.PlayerExperience);
				}

				if (captures.Info.ConsumedByCapture)
					self.Dispose();
			});
		}

		protected override void OnLastRun(Actor self)
		{
			CancelCapture(self);
			base.OnLastRun(self);
		}

		protected override void OnActorDispose(Actor self)
		{
			CancelCapture(self);
			base.OnActorDispose(self);
		}

		void CancelCapture(Actor self)
		{
			manager.CancelCapture(self, actor, targetManager);
		}

		public override Activity Tick(Actor self)
		{
			if (!targetManager.CanBeTargetedBy(actor, self, manager))
				Cancel(self);

			return base.Tick(self);
		}
	}
}
