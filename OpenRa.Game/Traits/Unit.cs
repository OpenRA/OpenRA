using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class UnitInfo : OwnedActorInfo, ITraitInfo
	{
		public readonly int ROT = 0;
		public readonly int Speed = 0;

		public object Create( Actor self ) { return new Unit( self ); }
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
