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

using System;
using System.Collections.Generic;
using OpenRa.GameRules;
using OpenRa.Graphics;
using OpenRa.Traits;

namespace OpenRa.Effects
{
	class Missile : IEffect
	{
		readonly Actor FiredBy;
		readonly WeaponInfo Weapon;
		readonly ProjectileInfo Projectile;
		readonly WarheadInfo Warhead;
		float2 Pos;
		readonly Actor Target;
		readonly Animation anim;
		int Facing;
		int t;
		int Altitude;

		public Missile(WeaponInfo weapon, Player owner, Actor firedBy,
			int2 src, Actor target, int altitude, int facing)
		{
			Weapon = weapon;
			Projectile = Rules.ProjectileInfo[Weapon.Projectile];
			Warhead = Rules.WarheadInfo[Weapon.Warhead];
			FiredBy = firedBy;
			Target = target;
			Pos = src.ToFloat2();
			Altitude = altitude;
			Facing = facing;

			if (Projectile.Image != null && Projectile.Image != "none")
			{
				if (Projectile.Rotates)
					anim = new Animation(Projectile.Image, () => Facing);
				else
					anim = new Animation(Projectile.Image);

				anim.PlayRepeating("idle");
			}
		}

		const int MissileCloseEnough = 7;
		const float Scale = .2f;

		public void Tick( World world )
		{
			t += 40;

			var targetUnit = Target.traits.GetOrDefault<Unit>();
			var targetAltitude = targetUnit != null ? targetUnit.Altitude : 0;
			Altitude += Math.Sign(targetAltitude - Altitude);

			Traits.Util.TickFacing(ref Facing, 
				Traits.Util.GetFacing(Target.CenterLocation - Pos, Facing),
				Projectile.ROT);

			anim.Tick();

			var dist = Target.CenterLocation - Pos;
			if (dist.LengthSquared < MissileCloseEnough * MissileCloseEnough || Target.IsDead)
			{
				world.AddFrameEndTask(w => w.Remove(this));

				if (t > Projectile.Arm * 40)	/* don't blow up in our launcher's face! */
					Combat.DoImpact(Pos.ToInt2(), Pos.ToInt2(), Weapon, Projectile, Warhead, FiredBy);
				return;
			}

			var speed = Scale * Weapon.Speed * ((targetAltitude > 0 && Weapon.TurboBoost) ? 1.5f : 1f);

			var angle = Facing / 128f * Math.PI;
			var move = speed * -float2.FromAngle((float)angle);
			Pos += move;

			if (Projectile.Trail != null)
				world.AddFrameEndTask(w => w.Add(
					new Smoke(w, (Pos - 1.5f * move - new int2( 0, Altitude )).ToInt2(), Projectile.Trail)));

			// todo: running out of fuel
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, Pos - 0.5f * anim.Image.size - new float2(0, Altitude), "effect");
		}
	}
}
