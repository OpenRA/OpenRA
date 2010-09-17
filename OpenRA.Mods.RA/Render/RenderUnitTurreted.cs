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
			var facing = self.Trait<IFacing>();
			var turreted = self.Trait<Turreted>();
			var attack = self.TraitOrDefault<AttackBase>();
			var attackInfo = self.Info.Traits.Get<AttackBaseInfo>();

			var turretAnim = new Animation(GetImage(self), () => turreted.turretFacing );
			turretAnim.Play( "turret" );

			for( var i = 0; i < attack.Turrets.Count; i++ )
			{
				var turret = attack.Turrets[i];
				anims.Add( "turret_{0}".F(i), 
					new AnimationWithOffset( turretAnim,
						() => Combat.GetTurretPosition( self, facing, turret ),
						null) { ZOffset = 70 });

				if (attackInfo.MuzzleFlash)
				{
					var muzzleFlash = new Animation(GetImage(self), () => turreted.turretFacing);
					muzzleFlash.PlayFetchIndex("muzzle",
						() => (int)(turret.Recoil * 5.9f)); /* hack: dumb crap */
					anims.Add("muzzle_flash_{0}".F(i),
						new AnimationWithOffset(muzzleFlash,
							() => Combat.GetTurretPosition(self, facing, turret),
							() => turret.Recoil <= 0));
				}
			}
		}
	}
}
