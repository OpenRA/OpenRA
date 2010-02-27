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
	public class Bullet : IEffect
	{
		readonly Player Owner;
		readonly Actor FiredBy;
		readonly WeaponInfo Weapon;
		readonly ProjectileInfo Projectile;
		readonly WarheadInfo Warhead;
		readonly int2 Src;
		readonly int2 Dest;
		readonly int2 VisualDest;
		readonly int SrcAltitude;
		readonly int DestAltitude;

		int t = 0;
		Animation anim;

		const int BaseBulletSpeed = 100;		/* pixels / 40ms frame */

		public Bullet(string weapon, Player owner, Actor firedBy,
			int2 src, int2 dest, int srcAltitude, int destAltitude)
			: this(Rules.WeaponInfo[weapon], owner, firedBy, src, dest, srcAltitude, destAltitude) { }

		/* src, dest are *pixel* coords */
		public Bullet(WeaponInfo weapon, Player owner, Actor firedBy, 
			int2 src, int2 dest, int srcAltitude, int destAltitude)
		{
			Owner = owner;
			FiredBy = firedBy;
			Src = src;
			Dest = dest;
			SrcAltitude = srcAltitude;
			DestAltitude = destAltitude;
			VisualDest = Dest + new int2(
						firedBy.World.CosmeticRandom.Next(-10, 10),
						firedBy.World.CosmeticRandom.Next(-10, 10));
			Weapon = weapon;
			Projectile = Rules.ProjectileInfo[Weapon.Projectile];
			Warhead = Rules.WarheadInfo[Weapon.Warhead];

			if (Projectile.Image != null && Projectile.Image != "none")
			{
				if (Projectile.Rotates)
					anim = new Animation(Projectile.Image, () => Traits.Util.GetFacing((dest - src).ToFloat2(), 0));
				else
					anim = new Animation(Projectile.Image);

				anim.PlayRepeating("idle");
			}
		}

		int TotalTime() { return (Dest - Src).Length * BaseBulletSpeed / Weapon.Speed; }

		public void Tick( World world )
		{
			t += 40;

			if (t > TotalTime())		/* remove finished bullets */
			{
				world.AddFrameEndTask(w => w.Remove(this));
				Combat.DoImpact(Dest, VisualDest - new int2( 0, DestAltitude ), 
					Weapon, Projectile, Warhead, FiredBy);
			}

			if (Projectile.Trail != null)
			{
				var at = (float)t / TotalTime();
				var altitude = float2.Lerp(SrcAltitude, DestAltitude, at);
				var pos = float2.Lerp(Src.ToFloat2(), VisualDest.ToFloat2(), at)
					- 0.5f * anim.Image.size - new float2(0, altitude);

				var highPos = (Projectile.High || Projectile.Arcing)
					? (pos - new float2(0, (VisualDest - Src).Length * height * 4 * at * (1 - at)))
					: pos;

				world.AddFrameEndTask(w => w.Add(
					new Smoke(w, highPos.ToInt2(), Projectile.Trail)));
			}
		}

		const float height = .1f;

		public IEnumerable<Renderable> Render()
		{
			if (anim != null)
			{
				var at = (float)t / TotalTime();

				var altitude = float2.Lerp(SrcAltitude, DestAltitude, at);
				var pos = float2.Lerp( Src.ToFloat2(), VisualDest.ToFloat2(), at)
					- 0.5f * anim.Image.size - new float2( 0, altitude );

				if (Projectile.High || Projectile.Arcing)
				{
					if (Projectile.Shadow)
						yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, "shadow");

					var highPos = pos - new float2(0, (VisualDest - Src).Length * height * 4 * at * (1 - at));

					yield return new Renderable(anim.Image, highPos - .5f * anim.Image.size, Owner.Palette);
				}
				else
					yield return new Renderable(anim.Image, pos - .5f * anim.Image.size, Projectile.UnderWater ? "shadow" : Owner.Palette);
			}
		}
	}
}
