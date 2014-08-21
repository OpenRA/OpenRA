#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Graphics;
using OpenRA.Traits;
using OpenRA.Mods.RA.Render;

namespace OpenRA.Mods.RA
{
	class ThrowsParticleInfo : ITraitInfo, Requires<RenderSimpleInfo>, Requires<IBodyOrientationInfo>
	{
		public readonly string Anim = null;

		[Desc("Initial position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Minimum distance to throw the particle")]
		public readonly WRange MinThrowRange = new WRange(256);

		[Desc("Maximum distance to throw the particle")]
		public readonly WRange MaxThrowRange = new WRange(768);

		[Desc("Minimum angle to throw the particle")]
		public readonly WAngle MinThrowAngle = WAngle.FromDegrees(30);

		[Desc("Maximum angle to throw the particle")]
		public readonly WAngle MaxThrowAngle = WAngle.FromDegrees(60);

		[Desc("Speed to throw the particle (horizontal WPos/tick)")]
		public readonly int Velocity = 75;

		[Desc("Maximum rotation rate")]
		public readonly float ROT = 15;

		public object Create(ActorInitializer init) { return new ThrowsParticle(init, this); }
	}

	class ThrowsParticle : ITick
	{
		WVec pos;
		WVec initialPos;
		WVec finalPos;
		WAngle angle;

		int tick = 0;
		int length;

		float facing;
		float rotation;

		public ThrowsParticle(ActorInitializer init, ThrowsParticleInfo info)
		{
			var self = init.self;
			var rs = self.Trait<RenderSimple>();
			var body = self.Trait<IBodyOrientation>();

			// TODO: Carry orientation over from the parent instead of just facing
			var bodyFacing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : 0;
			facing = Turreted.GetInitialTurretFacing(init, 0);

			// Calculate final position
			var throwRotation = WRot.FromFacing(Game.CosmeticRandom.Next(1024));
			var throwDistance = Game.CosmeticRandom.Next(info.MinThrowRange.Range, info.MaxThrowRange.Range);

			initialPos = pos = info.Offset.Rotate(body.QuantizeOrientation(self, WRot.FromFacing(bodyFacing)));
			finalPos = initialPos + new WVec(throwDistance, 0, 0).Rotate(throwRotation);
			angle = new WAngle(Game.CosmeticRandom.Next(info.MinThrowAngle.Angle, info.MaxThrowAngle.Angle));
			length = (finalPos - initialPos).Length / info.Velocity;

			// Facing rotation
			rotation = WRange.FromPDF(Game.CosmeticRandom, 2).Range * info.ROT / 1024;

			var anim = new Animation(init.world, rs.GetImage(self), () => (int)facing);
			anim.PlayRepeating(info.Anim);
			rs.Add(info.Anim, new AnimationWithOffset(anim, () => pos, null));
		}

		public void Tick(Actor self)
		{
			if (tick == length)
				return;

			pos = WVec.LerpQuadratic(initialPos, finalPos, angle, tick++, length);

			// Spin the particle
			facing += rotation;
			rotation *= .9f;
		}
	}
}
