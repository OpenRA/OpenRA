using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;
using IjwFramework.Types;
using OpenRa.Game.Graphics;

namespace OpenRa.Game
{
	interface IEffect
	{
		void Tick(Game g);
		IEnumerable<Pair<Sprite, float2>> Render();
		Player Owner { get; }
	}

	class Bullet : IEffect
	{
		public Player Owner { get; private set; }
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

		public void Tick(Game game)
		{
			if (t == 0)
				game.PlaySound(Weapon.Report + ".aud", false);

			t += 40;

			if (t > TotalTime())		/* remove finished bullets */
			{
				game.world.AddFrameEndTask(w => w.Remove(this));
				game.PlaySound("kaboom25.aud", false);
			}
		}

		public IEnumerable<Pair<Sprite, float2>> Render()
		{
			yield return Pair.New(anim.Image,
				float2.Lerp(
					Src.ToFloat2(), 
					Dest.ToFloat2(), 
					(float)t / TotalTime()) - 0.5f * anim.Image.size);
		}
	}
}
