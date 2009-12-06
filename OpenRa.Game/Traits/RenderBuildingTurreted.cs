
namespace OpenRa.Game.Traits
{
	class RenderBuildingTurreted : RenderBuilding
	{
		public RenderBuildingTurreted(Actor self)
			: base(self)
		{
			Make( () => PlayTurretAnim( self, "idle" ), self);
		}

		void PlayTurretAnim(Actor self, string a)
		{
			anim.PlayFacing(a, () => self.traits.Get<Turreted>().turretFacing);
		}

		public override void Damaged(Actor self, DamageState ds)
		{
			switch (ds)
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
