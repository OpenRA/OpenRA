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
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ThrowsParticleInfo : ITraitInfo
	{
		public readonly string Anim = null;
		public readonly int[] Offset = new[] { 0, 0, 0, 0 };
		public readonly int[] Spread = new[] { 0, 0 };
		public readonly float Speed = 20;
		public readonly string AnimKey = null;
		public readonly bool UseTurretFacing = true;
		public readonly float ROT = 15;

		public object Create(ActorInitializer init) { return new ThrowsParticle(this); }
	}

	class ThrowsParticle : ITick
	{
		ThrowsParticleInfo info;
		float2 pos;
		float alt;

		float2 v;
		float va;
		float facing;
		float dfacing;

		const float gravity = 1.3f;

		public ThrowsParticle(ThrowsParticleInfo info) { this.info = info; }
		public float? InitialFacing = null;
	
		public void Tick(Actor self)
		{
			if (info != null)
			{
				alt = 0;
				var move = self.traits.Get<IMove>();
				pos = Combat.GetTurretPosition(self, move, new Turret(info.Offset));
				var ru = self.traits.Get<RenderUnit>();

				v = Game.CosmeticRandom.Gauss2D(1) * info.Spread.RelOffset();
				dfacing = Game.CosmeticRandom.Gauss1D(2) * info.ROT;
				va = info.Speed;

				if (!info.UseTurretFacing) InitialFacing = null;
				facing = InitialFacing ?? move.Facing;

				var anim = new Animation(ru.GetImage(self), () => (int)facing);
				anim.PlayRepeating(info.Anim);

				ru.anims.Add(info.AnimKey, new RenderSimple.AnimationWithOffset(
					anim, () => pos - new float2(0, alt), null));

				info = null;
			}

			va -= gravity;
			alt += va;

			if (alt < 0) alt = 0;
			else
			{
				pos += v;
				v = .9f * v;

				facing += dfacing;
				dfacing *= .9f;
			}
		}
	}
}
