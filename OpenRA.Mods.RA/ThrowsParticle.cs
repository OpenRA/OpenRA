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
using OpenRA.Mods.RA.Render;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class ThrowsParticleInfo : ITraitInfo, Requires<RenderUnitInfo>
	{
		public readonly string Anim = null;
		public readonly int[] Offset = new[] { 0, 0, 0, 0 };
		public readonly int[] Spread = new[] { 0, 0 };
		public readonly float Speed = 20;
		public readonly string AnimKey = null;
		public readonly float ROT = 15;

		public object Create(ActorInitializer init) { return new ThrowsParticle(init, this); }
	}

	class ThrowsParticle : ITick
	{
		float2 pos;
		float alt;

		float2 v;
		float va;
		float facing;
		float dfacing;

		const float gravity = 1.3f;

		public ThrowsParticle(ActorInitializer init, ThrowsParticleInfo info)
		{
			var self = init.self;
			var ifacing = self.Trait<IFacing>();
			var ru = self.Trait<RenderUnit>();

			alt = 0;
			facing = Turreted.GetInitialTurretFacing( init, 0 );
			pos = new Turret(info.Offset).PxPosition(self, ifacing).ToFloat2();

			v = Game.CosmeticRandom.Gauss2D(1) * info.Spread.RelOffset();
			dfacing = Game.CosmeticRandom.Gauss1D(2) * info.ROT;
			va = info.Speed;

			var anim = new Animation(ru.GetImage(self), () => (int)facing);
			anim.PlayRepeating(info.Anim);

			ru.anims.Add(info.AnimKey, new AnimationWithOffset(
				anim, wr => pos - new float2(0, alt), null));
		}

		public void Tick(Actor self)
		{
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
