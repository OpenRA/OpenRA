#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderUnitTurretedInfo : RenderUnitInfo, Requires<TurretedInfo>, Requires<AttackBaseInfo>
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
			var attack = self.Trait<AttackBase>();

			var turretAnim = new Animation(GetImage(self), () => turreted.turretFacing );
			turretAnim.Play( "turret" );

			for( var i = 0; i < attack.Turrets.Count; i++ )
			{
				var turret = attack.Turrets[i];
				anims.Add( "turret_{0}".F(i),
					new AnimationWithOffset( turretAnim,
						() => Combat.GetTurretPosition( self, facing, turret ),
						null));
			}
		}
	}
}
