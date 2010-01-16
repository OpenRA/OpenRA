using OpenRa.GameRules;

namespace OpenRa.Traits
{
	class UnitInfo : OwnedActorInfo, ITraitInfo
	{
		public readonly int InitialFacing = 128;
		public readonly int ROT = 255;
		public readonly int Speed = 1;

		public object Create( Actor self ) { return new Unit( self ); }
	}

	public class Unit : INotifyDamage
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
					Sound.Play(self.Info.Traits.Get<OwnedActorInfo>().WaterBound 
						? "navylst1.aud" : "unitlst1.aud");
		}
	}
}
