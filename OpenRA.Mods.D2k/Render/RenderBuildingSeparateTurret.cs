#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

/*using System;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingSeparateTurretInfo : RenderBuildingInfo, Requires<TurretedInfo>, Requires<AttackBaseInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingSeparateTurret( init, this ); }
	}

	class RenderBuildingSeparateTurret : RenderBuilding
	{
		public RenderBuildingSeparateTurret( ActorInitializer init, RenderBuildingInfo info )
			: base(init, info, MakeTurretFacingFunc(init.self))
		{
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

		static Func<int> MakeTurretFacingFunc(Actor self)
		{
			var turreted = self.Trait<Turreted>();
			return () => turreted.turretFacing;
		}

	}
} */
