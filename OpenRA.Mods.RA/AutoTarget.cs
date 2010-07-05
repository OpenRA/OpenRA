#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class AutoTargetInfo : TraitInfo<AutoTarget>
	{
		public readonly float ScanTimeAverage = 2f;
		public readonly float ScanTimeSpread = .5f;
	}

	class AutoTarget : ITick, INotifyDamage
	{
		[Sync]
		int nextScanTime = 0;

		void AttackTarget(Actor self, Actor target)
		{
			var attack = self.traits.Get<AttackBase>();
			if (target != null)
				attack.ResolveOrder(self, new Order("Attack", self, target));
		}

		public void Tick(Actor self)
		{
			if (!self.IsIdle) return;

			if (--nextScanTime <= 0)
			{
				var attack = self.traits.Get<AttackBase>();
				var range = Combat.GetMaximumRange(self);

				if (!attack.target.IsValid ||
					(Util.CellContaining(attack.target.CenterLocation) - self.Location).LengthSquared > range * range)
					AttackTarget(self, ChooseTarget(self, range));

				var info = self.Info.Traits.Get<AutoTargetInfo>();
				nextScanTime = (int)(25 * (info.ScanTimeAverage + 
					(self.World.SharedRandom.NextDouble() * 2 - 1) * info.ScanTimeSpread));
			}
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = self.World.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return inRange
				.Where(a => a.Owner != null && self.Owner.Stances[ a.Owner ] == Stance.Enemy)
				.Where(a => Combat.HasAnyValidWeapons(self, Target.FromActor(a)))
				.Where(a => !a.traits.Contains<Cloak>() || !a.traits.Get<Cloak>().Cloaked)
				.OrderBy(a => (a.Location - self.Location).LengthSquared)
				.FirstOrDefault();
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (!self.IsIdle) return;

			if (!e.Attacker.Info.Traits.Contains<OwnedActorInfo>()) return;

			// not a lot we can do about things we can't hurt... although maybe we should automatically run away?
			if (!Combat.HasAnyValidWeapons(self, Target.FromActor(e.Attacker))) return;

			// don't retaliate against own units force-firing on us. it's usually not what the player wanted.
			if (self.Owner.Stances[e.Attacker.Owner] == Stance.Ally) return;

			if (e.Damage < 0) return;	// don't retaliate against healers

			AttackTarget(self, e.Attacker);
		}
	}
}
