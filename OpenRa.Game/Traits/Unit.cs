
namespace OpenRa.Game.Traits
{
	class Unit : INotifyDamage
	{
		public int Facing;
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
