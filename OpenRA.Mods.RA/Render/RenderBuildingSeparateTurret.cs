#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Buildings;

namespace OpenRA.Mods.RA.Render
{
	class RenderBuildingSeparateTurretInfo : RenderBuildingInfo, Requires<TurretedInfo>, Requires<AttackBaseInfo>
	{
		public override object Create(ActorInitializer init) { return new RenderBuildingSeparateTurret(init, this); }
	}

	class RenderBuildingSeparateTurret : RenderBuilding
	{
		public RenderBuildingSeparateTurret(ActorInitializer init, RenderBuildingInfo info)
			: base(init, info)
		{
			var turreted = init.self.Trait<Turreted>();
			var attack = init.self.Trait<AttackBase>();

			var turretAnim = new Animation(GetImage(init.self), () => turreted.turretFacing);
			turretAnim.Play("turret");

			for( var i = 0; i < attack.Turrets.Count; i++ )
			{
				var turret = attack.Turrets[i];
				anims.Add( "turret_{0}".F(i),
					new AnimationWithOffset(turretAnim,
						() => Combat.GetTurretPosition(init.self, null, turret).ToFloat2(),
						null));
			}
		}
	}
}