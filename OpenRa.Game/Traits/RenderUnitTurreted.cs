using System.Collections.Generic;
using System.Linq;
using OpenRa.Game.Graphics;

namespace OpenRa.Game.Traits
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
			var attack = self.traits.WithInterface<AttackBase>().FirstOrDefault();

			var turretAnim = new Animation(self.Info.Name);
			turretAnim.PlayFacing( "turret", () => turreted.turretFacing );

			if( self.Info.PrimaryOffset != null )
				anims.Add("turret_1", new AnimationWithOffset(
					turretAnim,
					() => Util.GetTurretPosition(self, unit, self.Info.PrimaryOffset, attack.primaryRecoil),
					null) { ZOffset = 1 });

			if( self.Info.SecondaryOffset != null )
				anims.Add("turret_2", new AnimationWithOffset(
					turretAnim,
					() => Util.GetTurretPosition(self, unit, self.Info.SecondaryOffset, attack.secondaryRecoil),
					null) { ZOffset = 1 });

			if( self.Info.MuzzleFlash )
			{
				var muzzleFlash = new Animation( self.Info.Name );
				muzzleFlash.PlayFetchIndex( "muzzle",
					() => ( Util.QuantizeFacing( self.traits.Get<Turreted>().turretFacing, 8 ) ) * 6
						+ (int)( attack.primaryRecoil * 5.9f ) ); /* hack: recoil can be 1.0f, but don't overflow into next anim */
				anims.Add( "muzzle_flash", new AnimationWithOffset(
					muzzleFlash,
					() => Util.GetTurretPosition( self, unit, self.Info.PrimaryOffset, attack.primaryRecoil ),
					() => attack.primaryRecoil <= 0 ) );
			}
		}
	}
}
