
namespace OpenRa.Traits
{
	class RenderBuildingTurretedInfo : RenderBuildingInfo
	{
		public override object Create(Actor self) { return new RenderBuildingTurreted(self); }
	}

	class RenderBuildingTurreted : RenderBuilding, INotifyBuildComplete
	{
		public RenderBuildingTurreted(Actor self)
			: base(self, () => self.traits.Get<Turreted>().turretFacing)
		{
		}

		public void BuildingComplete( Actor self )
		{
			anim.Play( "idle" );
		}

		public override void Damaged(Actor self, AttackInfo e)
		{
			if (!e.DamageStateChanged) return;

			switch (e.DamageState)
			{
				case DamageState.Normal:
					anim.Play( "idle" );
					break;
				case DamageState.Half:
					anim.Play( "damaged-idle" );
					Sound.Play("kaboom1.aud");
					break;
			}
		}
	}
}
