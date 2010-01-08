using System.Collections.Generic;
using OpenRa.Game.GameRules;
using OpenRa.Game.Graphics;
using OpenRa.Game.Traits;
using System;

namespace OpenRa.Game.Effects
{
	class Missile : IEffect
	{
		readonly Player Owner;
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

		public Missile(string weapon, Player owner, Actor firedBy,
			int2 src, Actor target, int altitude, int facing)
		{
			Weapon = Rules.WeaponInfo[weapon];
			Projectile = Rules.ProjectileInfo[Weapon.Projectile];
			Warhead = Rules.WarheadInfo[Weapon.Warhead];
			FiredBy = firedBy;
			Owner = owner;
			Target = target;
			Pos = src.ToFloat2();
			Altitude = altitude;
			Facing = facing;

			if (Projectile.Image != null && Projectile.Image != "none")
			{
				anim = new Animation(Projectile.Image);

				if (Projectile.Rotates)
					Traits.Util.PlayFacing(anim, "idle", () => Facing);
				else
					anim.PlayRepeating("idle");
			}
		}

		const int MissileCloseEnough = 7;
		const float Scale = .2f;

		public void Tick()
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
				Game.world.AddFrameEndTask(w => w.Remove(this));

				if (t > Projectile.Arm * 40)	/* don't blow up in our launcher's face! */
					Combat.DoImpact(Pos.ToInt2(), Pos.ToInt2(), Weapon, Projectile, Warhead, FiredBy);
				return;
			}

			var speed = Scale * Weapon.Speed * ((targetAltitude > 0 && Weapon.TurboBoost) ? 1.5f : 1f);

			var angle = Facing / 128f * Math.PI;
			var move = speed * -float2.FromAngle((float)angle);
			Pos += move;

			if (Projectile.Animates)
				Game.world.AddFrameEndTask(w => w.Add(new Smoke((Pos - 1.5f * move - new int2( 0, Altitude )).ToInt2())));

			// todo: running out of fuel
		}

		public IEnumerable<Renderable> Render()
		{
			yield return new Renderable(anim.Image, Pos - 0.5f * anim.Image.size - new float2(0, Altitude), 0);
		}
	}
}
