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
			var self = init.self;
			var turreted = self.TraitsImplementing<Turreted>();

			var i = 0;
			foreach (var t in turreted)
			{
				var anim = new Animation(GetImage(self), () => t.turretFacing);
				anim.Play("turret");

				anims.Add("turret_{0}".F(i++), new AnimationWithOffset(anim,
					wr => wr.ScreenPxOffset(t.Position(self)), null));
			}
		}
	}
}