#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class ThrowsParticleInfo : TraitInfo, Requires<WithSpriteBodyInfo>, Requires<BodyOrientationInfo>
	{
		[FieldLoader.Require]
		public readonly string Anim = null;

		[Desc("Initial position relative to body")]
		public readonly WVec Offset = WVec.Zero;

		[Desc("Minimum distance to throw the particle")]
		public readonly WDist MinThrowRange = new WDist(256);

		[Desc("Maximum distance to throw the particle")]
		public readonly WDist MaxThrowRange = new WDist(768);

		[Desc("Minimum angle to throw the particle")]
		public readonly WAngle MinThrowAngle = WAngle.FromDegrees(30);

		[Desc("Maximum angle to throw the particle")]
		public readonly WAngle MaxThrowAngle = WAngle.FromDegrees(60);

		[Desc("Speed to throw the particle (horizontal WPos/tick)")]
		public readonly int Velocity = 75;

		[Desc("Speed at which the particle turns.")]
		public readonly WAngle TurnSpeed = new WAngle(60);

		public override object Create(ActorInitializer init) { return new ThrowsParticle(init, this); }
	}

	class ThrowsParticle : ITick
	{
		WVec pos;
		readonly WVec initialPos;
		readonly WVec finalPos;
		readonly WAngle angle;

		int tick = 0;
		readonly int length;

		WAngle facing;
		WAngle rotation;
		readonly int direction;

		public ThrowsParticle(ActorInitializer init, ThrowsParticleInfo info)
		{
			var self = init.Self;
			var rs = self.Trait<RenderSprites>();
			var body = self.Trait<BodyOrientation>();

			// TODO: Carry orientation over from the parent instead of just facing
			var dynamicFacingInit = init.GetOrDefault<DynamicFacingInit>();
			var bodyFacing = dynamicFacingInit != null ? dynamicFacingInit.Value() : init.GetValue<FacingInit, WAngle>(WAngle.Zero);
			facing = TurretedInfo.WorldFacingFromInit(init, info, WAngle.Zero)();

			// Calculate final position
			var throwRotation = WRot.FromYaw(new WAngle(Game.CosmeticRandom.Next(1024)));
			var throwDistance = Game.CosmeticRandom.Next(info.MinThrowRange.Length, info.MaxThrowRange.Length);

			initialPos = pos = info.Offset.Rotate(body.QuantizeOrientation(WRot.FromYaw(bodyFacing)));
			finalPos = initialPos + new WVec(throwDistance, 0, 0).Rotate(throwRotation);
			angle = new WAngle(Game.CosmeticRandom.Next(info.MinThrowAngle.Angle, info.MaxThrowAngle.Angle));
			length = (finalPos - initialPos).Length / info.Velocity;

			// WAngle requires positive inputs, so track the speed and direction separately
			var rotationSpeed = WDist.FromPDF(Game.CosmeticRandom, 2).Length * info.TurnSpeed.Angle / 1024;
			direction = rotationSpeed < 0 ? -1 : 1;
			rotation = new WAngle(Math.Abs(rotationSpeed));

			var anim = new Animation(init.World, rs.GetImage(self), () => facing);
			anim.PlayRepeating(info.Anim);
			rs.Add(new AnimationWithOffset(anim, () => pos, null));
		}

		void ITick.Tick(Actor self)
		{
			if (tick >= length)
				return;

			pos = WVec.LerpQuadratic(initialPos, finalPos, angle, tick++, length);

			// Spin the particle
			facing += new WAngle(direction * rotation.Angle);
			rotation = new WAngle(rotation.Angle * 90 / 100);
		}
	}
}
