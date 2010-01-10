using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class UnitInfo : ITraitInfo
	{
		public readonly int HP = 0;
		public readonly ArmorType Armor = ArmorType.none;
		public readonly bool Crewed = false;		// replace with trait?

		public object Create(Actor self) { return new Unit(self); }
	}

	class Unit : INotifyDamage
	{
		[Sync]
		public int Facing;
		[Sync]
		public int Altitude;

		public Unit( Actor self ) { }

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				if (self.Owner == Game.LocalPlayer)
					Sound.Play("unitlst1.aud");
		}
	}
}
