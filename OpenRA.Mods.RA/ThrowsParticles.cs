#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using OpenRA.Graphics;
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
				pos = Traits.Util.GetTurretPosition(self, self.traits.Get<Unit>(), info.Offset, 0);
				var ru = self.traits.Get<RenderUnit>();

				v = Game.CosmeticRandom.Gauss2D(1) * info.Spread.RelOffset();
				dfacing = Game.CosmeticRandom.Gauss1D(2) * info.ROT;
				va = info.Speed;

				if (!info.UseTurretFacing) InitialFacing = null;
				facing = InitialFacing ?? self.traits.Get<Unit>().Facing;

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
