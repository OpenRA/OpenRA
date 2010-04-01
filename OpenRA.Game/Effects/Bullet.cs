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

using System.Collections.Generic;
using OpenRA.GameRules;
using OpenRA.Graphics;
using OpenRA.Traits;

namespace OpenRA.Effects
{
	public class BulletInfo : IProjectileInfo
	{
		public readonly int Speed = 1;
		public readonly string Trail = null;
		public readonly bool Inaccurate = false;
		public readonly string Image = null;
		public readonly bool High = false;
		public readonly bool Arcing = false;
		public readonly int RangeLimit = 0;
		public readonly int Arm = 0;
		public readonly bool Shadow = false;
		public readonly bool Proximity = false;

		public IEffect Create(ProjectileArgs args) { return new Bullet( this, args ); }
	}

	public class Bullet : IEffect
	{
		readonly BulletInfo Info;
		readonly ProjectileArgs Args;
		readonly int2 VisualDest;
		
		int t = 0;
		Animation anim;

		const int BaseBulletSpeed = 100;		/* pixels / 40ms frame */

		public Bullet(BulletInfo info, ProjectileArgs args)
		{
			Info = info;
			Args = args;

			VisualDest = args.dest + new int2(
						args.firedBy.World.CosmeticRandom.Next(-10, 10),
						args.firedBy.World.CosmeticRandom.Next(-10, 10));

			if (Info.Image != null)
			{
				anim = new Animation(Info.Image, () => Traits.Util.GetFacing(Args.dest - Args.src, 0));
				anim.PlayRepeating("idle");
			}
		}

		int TotalTime() { return (Args.dest - Args.src).Length * BaseBulletSpeed / Info.Speed; }

		public void Tick( World world )
		{
			t += 40;

			if (t > TotalTime()) Explode( world );

			if (Info.Trail != null)
			{
				var at = (float)t / TotalTime();
				var altitude = float2.Lerp(Args.srcAltitude, Args.destAltitude, at);
				var pos = float2.Lerp(Args.src, VisualDest, at)
					- 0.5f * anim.Image.size - new float2(0, altitude);

				var highPos = (Info.High || Info.Arcing)
					? (pos - new float2(0, (VisualDest - Args.src).Length * height * 4 * at * (1 - at)))
					: pos;

				world.AddFrameEndTask(w => w.Add(
					new Smoke(w, highPos.ToInt2(), Info.Trail)));
			}
		}

		const float height = .1f;

		public IEnumerable<Renderable> Render()
		{
			if (anim != null)
			{
				var at = (float)t / TotalTime();

				var altitude = float2.Lerp(Args.srcAltitude, Args.destAltitude, at);
				var pos = float2.Lerp( Args.src, VisualDest, at)
					- 0.5f * anim.Image.size - new float2( 0, altitude );

				if (Info.High || Info.Arcing)
				{
					if (Info.Shadow)
						yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, "shadow");

					var highPos = pos - new float2(0, (VisualDest - Args.src).Length * height * 4 * at * (1 - at));

					yield return new Renderable(anim.Image, highPos - .5f * anim.Image.size, Args.firedBy.Owner.Palette);
				}
				else
					yield return new Renderable(anim.Image, pos - .5f * anim.Image.size,
						Args.weapon.Underwater ? "shadow" : Args.firedBy.Owner.Palette);
			}
		}

		void Explode( World world )
		{
			world.AddFrameEndTask(w => w.Remove(this));
			Combat.DoImpacts(Args, VisualDest - new int2(0, Args.destAltitude));
		}
	}
}
