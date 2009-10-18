using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using IjwFramework.Types;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	class Bullet
	{
		public readonly Player Owner;
		readonly Actor FiredBy;
		readonly WeaponInfo Weapon;
		readonly ProjectileInfo Projectile;
		readonly WarheadInfo Warhead;
		readonly int2 Src;
		readonly int2 Dest;

		int t = 0;
		Animation anim;

		const int BaseBulletSpeed = 100;		/* pixels / 40ms frame */

		/* src, dest are *pixel* coords */
		public Bullet(string weapon, Player owner, Actor firedBy, 
			int2 src, int2 dest, Game game)
		{
			Owner = owner;
			FiredBy = firedBy;
			Src = src;
			Dest = dest;
			Weapon = Rules.WeaponInfo[weapon];
			Projectile = Rules.ProjectileInfo[Weapon.Projectile];
			Warhead = Rules.WarheadInfo[Weapon.Warhead];

			anim = new Animation(Projectile.Image);
			anim.PlayRepeating("idle");
		}

		int TotalTime() { return (Dest - Src).Length * BaseBulletSpeed / Weapon.Speed; }

		public void Tick(Game game, int dt)
		{
			if (t == 0)
				game.PlaySound(Weapon.Report + ".aud", false);

			t += dt;
			if (t > TotalTime())
				t = 0;	/* temporary! loop the bullet forever */
		}

		public IEnumerable<Pair<Sprite, float2>> Render()
		{
			yield return Pair.New(anim.Image,
				float2.Lerp(
					Src.ToFloat2(), 
					Dest.ToFloat2(), 
					(float)t / TotalTime()));
		}
	}
}
