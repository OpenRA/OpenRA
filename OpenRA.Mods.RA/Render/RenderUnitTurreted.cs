#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderUnitTurretedInfo : RenderUnitInfo
	{
		public override object Create(ActorInitializer init) { return new RenderUnitTurreted(init.self); }
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
					() => Combat.GetTurretPosition(self, unit, attackInfo.PrimaryOffset, attack.primaryRecoil),
					null) { ZOffset = 1 });

			if (attackInfo.SecondaryOffset != null)
				anims.Add("turret_2", new AnimationWithOffset(
					turretAnim,
					() => Combat.GetTurretPosition(self, unit, attackInfo.SecondaryOffset, attack.secondaryRecoil),
					null) { ZOffset = 1 });

			if( attackInfo.MuzzleFlash )
			{
				var muzzleFlash = new Animation( GetImage(self), () => self.traits.Get<Turreted>().turretFacing );
				muzzleFlash.PlayFetchIndex( "muzzle",
					() => (int)( attack.primaryRecoil * 5.9f ) ); /* hack: recoil can be 1.0f, but don't overflow into next anim */
				anims.Add( "muzzle_flash", new AnimationWithOffset(
					muzzleFlash,
					() => Combat.GetTurretPosition(self, unit, attackInfo.PrimaryOffset, attack.primaryRecoil),
					() => attack.primaryRecoil <= 0 ) );
			}
		}
	}
}
