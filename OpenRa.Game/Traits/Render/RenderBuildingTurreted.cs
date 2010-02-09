
namespace OpenRa.Traits
{
	class RenderBuildingTurretedInfo : RenderBuildingInfo
	{
		public override object Create(Actor self) { return new RenderBuildingTurreted(self); }
	}

	class RenderBuildingTurreted : RenderBuilding, INotifyBuildComplete
	{
		public RenderBuildingTurreted(Actor self)
			: base(self)
		{
		}

		public void BuildingComplete( Actor self )
		{
			PlayTurretAnim( self, "idle" );
		}

		void PlayTurretAnim( Actor self, string a )
		{
			anim.PlayFacing( a, () => self.traits.Get<Turreted>().turretFacing );
		}

		public override void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;

			switch (e.DamageState)
			{
				case DamageState.Normal:
					PlayTurretAnim(self, "idle");
					break;
				case DamageState.Half:
					PlayTurretAnim(self, "damaged-idle");
					Sound.Play("kaboom1.aud");
					break;
			}
		}
	}
}
