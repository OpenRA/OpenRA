using OpenRa.Game.GameRules;

namespace OpenRa.Game.Traits
{
	class Building : INotifyDamage
	{
		public readonly BuildingInfo unitInfo;

		public Building(Actor self)
		{
			unitInfo = (BuildingInfo)self.Info;
			self.CenterLocation = Game.CellSize 
				* ((float2)self.Location + .5f * (float2)unitInfo.Dimensions);
		}

		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				Sound.Play("kaboom22.aud");
		}
	}
}
