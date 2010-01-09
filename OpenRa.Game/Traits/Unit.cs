
namespace OpenRa.Game.Traits
{
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
