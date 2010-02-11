using System.Collections.Generic;
using System.Linq;
using OpenRa.Graphics;

namespace OpenRa.Traits
{
	class RenderUnitTurretedInfo : RenderUnitInfo
	{
		public override object Create(Actor self) { return new RenderUnitTurreted(self); }
	}

	class RenderUnitTurreted : RenderUnit
	{
		public RenderUnitTurreted(Actor self)
			: base(self)
		{
			var unit = self.traits.Get<Unit>();
			var turreted = self.traits.Get<Turreted>();
			var attack = self.traits.GetOrDefault<AttackBase>();
			var attackInfo = self.Info.Traits.Get<AttackBaseInfo>();

			var turretAnim = new Animation(GetImage(self), () => turreted.turretFacing );
			turretAnim.Play( "turret" );

			if( attackInfo.PrimaryOffset != null )
				anims.Add("turret_1", new AnimationWithOffset(
					turretAnim,
					() => Util.GetTurretPosition(self, unit, attackInfo.PrimaryOffset, attack.primaryRecoil),
					null) { ZOffset = 1 });

			if (attackInfo.SecondaryOffset != null)
				anims.Add("turret_2", new AnimationWithOffset(
					turretAnim,
					() => Util.GetTurretPosition(self, unit, attackInfo.SecondaryOffset, attack.secondaryRecoil),
					null) { ZOffset = 1 });

			if( attackInfo.MuzzleFlash )
			{
				var muzzleFlash = new Animation( GetImage(self), () => self.traits.Get<Turreted>().turretFacing );
				muzzleFlash.PlayFetchIndex( "muzzle",
					() => (int)( attack.primaryRecoil * 5.9f ) ); /* hack: recoil can be 1.0f, but don't overflow into next anim */
				anims.Add( "muzzle_flash", new AnimationWithOffset(
					muzzleFlash,
					() => Util.GetTurretPosition(self, unit, attackInfo.PrimaryOffset, attack.primaryRecoil),
					() => attack.primaryRecoil <= 0 ) );
			}
		}
	}
}
