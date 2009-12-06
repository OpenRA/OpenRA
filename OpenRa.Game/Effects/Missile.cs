using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.Graphics;
using OpenRa.Game.GameRules;

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

		public Missile(string weapon, Player owner, Actor firedBy,
			int2 src, Actor target)
		{
			Weapon = Rules.WeaponInfo[weapon];
			Projectile = Rules.ProjectileInfo[Weapon.Projectile];
			Warhead = Rules.WarheadInfo[Weapon.Warhead];
			FiredBy = firedBy;
			Owner = owner;
			Target = target;
			Pos = src.ToFloat2();

			/* todo: initial facing should be turret facing, or unit facing if we're not turreted */
			Facing = Traits.Util.GetFacing( Target.CenterLocation - src.ToFloat2(), 0 );

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
		const float Scale = .3f;

		public void Tick()
		{
			if (t == 0)
				Sound.Play(Weapon.Report + ".aud");

			t += 40;

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

			var move = (Scale * Weapon.Speed / dist.Length) * dist;
			Pos += move;

			if (Projectile.Animates)
				Game.world.AddFrameEndTask(w => w.Add(new Smoke((Pos - 1.5f * move).ToInt2())));

			// todo: running out of fuel
			// todo: turbo boost vs aircraft
		}

		public IEnumerable<Tuple<Sprite, float2, int>> Render()
		{
			yield return Tuple.New(anim.Image, Pos - 0.5f * anim.Image.size, 0);
		}
	}
}
