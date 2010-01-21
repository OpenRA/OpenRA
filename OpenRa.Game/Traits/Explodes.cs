using OpenRa.Effects;

namespace OpenRa.Traits
{
	class ExplodesInfo : StatelessTraitInfo<Explodes> { }

	class Explodes : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			if (self.IsDead)
			{
				var unit = self.traits.GetOrDefault<Unit>();
				var altitude = unit != null ? unit.Altitude : 0;

				self.World.AddFrameEndTask(
					w => w.Add(new Bullet("UnitExplode", e.Attacker.Owner, e.Attacker,
						self.CenterLocation.ToInt2(), self.CenterLocation.ToInt2(),
						altitude, altitude)));
			}
		}
	}
}
