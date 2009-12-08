using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRa.Game.Traits
{
	class AutoTarget : ITick
	{
		public AutoTarget(Actor self) {}

		public void Tick(Actor self)
		{
			var attack = self.traits.WithInterface<AttackBase>().First();
			
			var range = Rules.WeaponInfo[self.Info.Primary].Range;
			if (attack.target == null || 
				(attack.target.Location - self.Location).LengthSquared > range * range + 2)
				attack.target = ChooseTarget(self, range);
		}

		Actor ChooseTarget(Actor self, float range)
		{
			var inRange = Game.FindUnitsInCircle(self.CenterLocation, Game.CellSize * range);

			return inRange.Where(a => a.Owner != self.Owner)	/* todo: one day deal with friendly players */
				.OrderBy(a => (a.Location - self.Location).LengthSquared)
				.FirstOrDefault();
		}
	}
}
