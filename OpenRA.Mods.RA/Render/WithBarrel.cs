#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Render
{
	[Desc("Renders barrels for units with the Turreted trait.")]
	class WithBarrelInfo : ITraitInfo, IRenderActorPreviewSpritesInfo, Requires<RenderSpritesInfo>, Requires<IBodyOrientationInfo>
	{
		[Desc("Sequence name to use")]
		public readonly string Sequence = "barrel";

		[Desc("Armament to use for recoil")]
		public readonly string Armament = "primary";

		[Desc("Turreted 'Barrel' key to display")]
		public readonly string Barrel = "first";

		[Desc("Visual offset")]
		public readonly WVec LocalOffset = WVec.Zero;

		public object Create(ActorInitializer init) { return new WithBarrel(init.self, this); }

		public IEnumerable<IActorPreview> RenderPreviewSprites(ActorPreviewInitializer init, RenderSpritesInfo rs, string image, int facings, PaletteReference p)
		{
			var body = init.Actor.Traits.Get<BodyOrientationInfo>();
			var armament = init.Actor.Traits.WithInterface<ArmamentInfo>()
				.First(a => a.Name == Armament);
			var t = init.Actor.Traits.WithInterface<TurretedInfo>()
				.First(tt => tt.Turret == armament.Turret);

			var anim = new Animation(init.World, image, () => t.InitialFacing);
			anim.Play(Sequence);

			var turretOrientation = body.QuantizeOrientation(new WRot(WAngle.Zero, WAngle.Zero, WAngle.FromFacing(t.InitialFacing)), facings);
			var turretOffset = body.LocalToWorld(t.Offset.Rotate(turretOrientation));

			yield return new SpriteActorPreview(anim, turretOffset, turretOffset.Y + turretOffset.Z, p, rs.Scale);
		}
	}

	class WithBarrel
	{
		WithBarrelInfo info;
		Actor self;
		Armament armament;
		Turreted turreted;
		IBodyOrientation body;
		Animation anim;

		public WithBarrel(Actor self, WithBarrelInfo info)
		{
			this.self = self;
			this.info = info;
			body = self.Trait<IBodyOrientation>();
			armament = self.TraitsImplementing<Armament>()
				.First(a => a.Info.Name == info.Armament);
			turreted = self.TraitsImplementing<Turreted>()
				.First(tt => tt.Name == armament.Info.Turret);

			var rs = self.Trait<RenderSprites>();
			anim = new Animation(self.World, rs.GetImage(self), () => turreted.TurretFacing);
			anim.Play(info.Sequence);
			rs.Add("barrel_{0}".F(info.Barrel), new AnimationWithOffset(
				anim, () => BarrelOffset(), null, () => false, p => WithTurret.ZOffsetFromCenter(self, p, 0)));

			// Restrict turret facings to match the sprite
			turreted.QuantizedFacings = anim.CurrentSequence.Facings;
		}

		WVec BarrelOffset()
		{
			var localOffset = info.LocalOffset + new WVec(-armament.Recoil, WRange.Zero, WRange.Zero);
			var turretOffset = turreted != null ? turreted.Position(self) : WVec.Zero;
			var turretOrientation = turreted != null ? turreted.LocalOrientation(self) : WRot.Zero;

			var quantizedBody = body.QuantizeOrientation(self, self.Orientation);
			var quantizedTurret =  body.QuantizeOrientation(self, turretOrientation);
			return turretOffset + body.LocalToWorld(localOffset.Rotate(quantizedTurret).Rotate(quantizedBody));
		}

		IEnumerable<WRot> BarrelRotation()
		{
			var b = self.Orientation;
			var qb = body.QuantizeOrientation(self, b);
			yield return turreted.LocalOrientation(self) + WRot.FromYaw(b.Yaw - qb.Yaw);
			yield return qb;
		}
	}
}
