#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ThrowsParticleInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		public readonly string Anim = null;

		[Desc("Initial position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Maximum distance to throw the particle")]
		public readonly WRange ThrowRange = new WRange(768);

		[Desc("Maximum height to throw the particle")]
		public readonly WRange ThrowHeight = new WRange(256);

		[Desc("Number of ticks to animate")]
		public readonly int Length = 15;

		[Desc("Maximum rotation rate")]
		public readonly float ROT = 15;

		public object Create(ActorInitializer init) { return new ThrowsParticle(init, this); }
	}

	class ThrowsParticle : ITick
	{
		ThrowsParticleInfo info;
		WVec pos;
		WVec initialPos;
		WVec finalPos;
		int tick = 0;

		float facing;
		float rotation;

		public ThrowsParticle(ActorInitializer init, ThrowsParticleInfo info)
		{
			this.info = info;

			var self = init.self;
			var rs = self.Trait<RenderSimple>();

			// TODO: Carry orientation over from the parent instead of just facing
			var bodyFacing = init.Contains<FacingInit>() ? init.Get<FacingInit,int>() : 0;
			facing = Turreted.GetInitialTurretFacing(init, 0);

			// Calculate final position
			var throwRotation = WRot.FromFacing(Game.CosmeticRandom.Next(1024));
			var throwOffset = new WVec((int)(Game.CosmeticRandom.Gauss1D(1)*info.ThrowRange.Range), 0, 0).Rotate(throwRotation);

			initialPos = pos = info.Offset.Rotate(rs.QuantizeOrientation(self, WRot.FromFacing(bodyFacing)));
			finalPos = initialPos + throwOffset;

			// Facing rotation
			rotation = Game.CosmeticRandom.Gauss1D(2) * info.ROT;

			var anim = new Animation(rs.GetImage(self), () => (int)facing);
			anim.PlayRepeating(info.Anim);
			rs.anims.Add(info.Anim, new AnimationWithOffset(anim, () => pos, null));
		}

		public void Tick(Actor self)
		{
			if (tick == info.Length)
				return;
			tick++;

			// Lerp position horizontally and height along a sinusoid using a cubic ease
			var t = (tick*tick*tick / (info.Length*info.Length) - 3*tick*tick / info.Length + 3*tick);
			var tp = WVec.Lerp(initialPos, finalPos, t, info.Length);
			var th = new WAngle(512*(info.Length - t) / info.Length).Sin()*info.ThrowHeight.Range / 1024;
			pos = new WVec(tp.X, tp.Y, th);

			// Spin the particle
			facing += rotation;
			rotation *= .9f;
		}
	}
}
