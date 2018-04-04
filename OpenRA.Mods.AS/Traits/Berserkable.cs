#region Copyright & License Information
/*
 * Copyright 2015- OpenRA.Mods.AS Developers (see AUTHORS)
 * This file is a part of a third-party plugin for OpenRA, which is
 * free software. It is made available to you under the terms of the
 * GNU General Public License as published by the Free Software
 * Foundation. For more information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.AS.Traits
{
	[Desc("When enabled, the actor will randomly try to attack nearby other actors.")]
	public class BerserkableInfo : ConditionalTraitInfo
	{
		public override object Create(ActorInitializer init) { return new Berserkable(init.Self, this); }
	}

	class Berserkable : ConditionalTrait<BerserkableInfo>, INotifyIdle
	{
		public Berserkable(Actor self, BerserkableInfo info)
			: base(info) { }

		void Blink(Actor self)
		{
			self.World.IssueOrder(new Order("Stop", self, false));
			self.World.AddFrameEndTask(w => { w.Remove(self); self.Generation++; w.Add(self); });
		}

		protected override void TraitEnabled(Actor self)
		{
			// Getting enraged cancels current activity.
			Blink(self);
		}

		protected override void TraitDisabled(Actor self)
		{
			// Getting unraged should drop the target, too.
			Blink(self);
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