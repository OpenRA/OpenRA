#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
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
			var turreted = self.TraitsImplementing<Turreted>();

			var i = 0;
			foreach (var t in turreted)
			{
				var turret = t;

				var anim = new Animation(GetImage(self), () => turret.turretFacing);
				anim.Play("turret");

				anims.Add("turret_{0}".F(i++), new AnimationWithOffset(anim,
					() => TurretPosition(self, turret, facing), null));
			}
		}

		float2 TurretPosition(Actor self, Turreted t, IFacing facing)
		{
			var recoil = self.TraitsImplementing<Armament>()
				.Where(w => w.Info.Turret == t.Name)
				.Aggregate(WRange.Zero, (a,b) => a + b.Recoil);

			var localOffset = new WVec(-recoil, WRange.Zero, WRange.Zero);
			var bodyOrientation = QuantizeOrientation(self, self.Orientation);
			var turretOrientation = QuantizeOrientation(self, t.LocalOrientation(self));
			var worldPos = WPos.Zero + t.Position(self) + LocalToWorld(localOffset.Rotate(turretOrientation).Rotate(bodyOrientation));

			return PPos.FromWPosHackZ(worldPos).ToFloat2();
		}
	}
}
