#region Copyright & License Information
/*
 * Written by Boolbada of OP Mod,
 * Follows GPLv3 License as OpenRA main engine:
 * 
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

/*
 * Almost works without base engine modification
 * While AttackBase.cs:UnforcedAttackTargetStances() affects target selection,
 * I don't think it is a good idea to mess with the very core part of the engine.
 * 
 * Unfortunately, due to CancelActivity not canceling the attack base's attack,
 * OnStopOrder of AttckBase and AttackFollow is made public for this.
 * A neater fix would be to go into Cancel() of the attack activities then
 * call OnStopOrder from there but that brings more changes to the base engine
 * so I chose to make it public. From modder's side, less change == better.
 */

namespace OpenRA.Mods.Yupgi_alert.Traits
{
	[Desc("Can be enraged by rage generators? (as in Kane's Wrath?)")]
	public class RageSusceptibleInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new RageSusceptible(init.Self, this); }
	}

	class RageSusceptible : ConditionalTrait<RageSusceptibleInfo>, INotifyIdle
	{
		public RageSusceptible(Actor self, RageSusceptibleInfo info)
			: base(info)
		{
		}

		void StopOrder(Actor self)
		{
			self.CancelActivity();

			// CancelActivity doesn't cancel attack orders as it is the
			// attack base TRAIT that's doing the attack. Let it drop target.
			var atbs = self.TraitsImplementing<AttackBase>().Where(a => !a.IsTraitDisabled).ToArray();
			if (atbs.Length == 0)
				return;

			foreach (var atb in atbs)
				atb.OnStopOrder(self);
		}

		protected override void TraitEnabled(Actor self)
		{
			// Getting enraged cancels current activity.
			StopOrder(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			// Getting unraged should drop the target, too.
			StopOrder(self);
		}

		WDist GetScanRange(Actor self, AttackBase[] atbs)
		{
			WDist range = WDist.Zero;

			// Get max value of autotarget scan range.
			var autoTargets = self.TraitsImplementing<AutoTarget>().Where(a => !a.IsTraitDisabled).ToArray();
			foreach (var at in autoTargets)
			{
				var r = at.Info.ScanRadius;
				if (r > range.Length)
					range = WDist.FromCells(r);
			}

			// Get maxrange weapon.
			foreach (var atb in atbs)
			{
				var r = atb.GetMaximumRange();
				if (r.Length > range.Length)
					range = r;
			}

			return range;
		}

		void INotifyIdle.TickIdle(Actor self)
		{
			if (IsTraitDisabled)
				return;

			var atbs = self.TraitsImplementing<AttackBase>().Where(a => !a.IsTraitDisabled).ToArray();
			if (atbs.Length == 0)
			{
				self.QueueActivity(new Wait(15));
				return;
			}

			WDist range = GetScanRange(self, atbs);

			var targets = self.World.FindActorsInCircle(self.CenterPosition, range)
				.Where(a => !a.Owner.NonCombatant && a != self && a.IsTargetableBy(self));

			if (!targets.Any())
			{
				self.QueueActivity(new Wait(15));
				return;
			}

			// Attack a random target.
			var target = Target.FromActor(targets.Random(self.World.SharedRandom));
			self.QueueActivity(atbs.First().GetAttackActivity(self, target, true, true));
		}
	}
}
