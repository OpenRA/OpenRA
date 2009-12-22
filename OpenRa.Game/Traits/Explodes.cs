using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Effects;

namespace OpenRa.Game.Traits
{
	class Explodes : INotifyDamage
	{
		public Explodes(Actor self) {}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead)
			{
				var unit = self.traits.GetOrDefault<Unit>();
				var altitude = unit != null ? unit.Altitude : 0;

				Game.world.AddFrameEndTask(
					w => w.Add(new Bullet("UnitExplode", e.Attacker.Owner, e.Attacker,
						self.CenterLocation.ToInt2(), self.CenterLocation.ToInt2(),
						altitude, altitude)));
			}
		}
	}
}
