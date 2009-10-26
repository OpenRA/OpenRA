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
		void Tick();
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
			int2 src, int2 dest)
		{
			Owner = owner;
			FiredBy = firedBy;
			Src = src;
			Dest = dest;
			Weapon = Rules.WeaponInfo[weapon];
			Projectile = Rules.ProjectileInfo[Weapon.Projectile];
			Warhead = Rules.WarheadInfo[Weapon.Warhead];

			if (Projectile.Image != null && Projectile.Image != "none")
			{
				anim = new Animation(Projectile.Image);
				anim.PlayRepeating("idle");
			}
		}

		int TotalTime() { return (Dest - Src).Length * BaseBulletSpeed / Weapon.Speed; }

		public void Tick()
		{
			if (t == 0)
				Game.PlaySound(Weapon.Report + ".aud", false);

			t += 40;

			if (t > TotalTime())		/* remove finished bullets */
			{
				Game.world.AddFrameEndTask(w => w.Remove(this));
				Game.world.AddFrameEndTask(w => w.Add(new Explosion(Dest)));

				var maxSpread = GetMaximumSpread();
				var hitActors = Game.FindUnitsInCircle(Dest, GetMaximumSpread());

				foreach (var victim in hitActors)
					victim.InflictDamage(FiredBy, this, (int)GetDamageToInflict(victim));
			}
		}

		public IEnumerable<Pair<Sprite, float2>> Render()
		{
			if (anim != null)
				yield return Pair.New(anim.Image,
					float2.Lerp(
						Src.ToFloat2(),
						Dest.ToFloat2(),
						(float)t / TotalTime()) - 0.5f * anim.Image.size);
		}

		float GetMaximumSpread()
		{
			return (int)(Warhead.Spread * Math.Log(Weapon.Damage, 2));
		}

		float GetDamageToInflict(Actor target)
		{
			if( target.unitInfo == null ) // tree or other doodad
				return 0;

			/* todo: some things can't be damaged AT ALL by certain weapons! */
			var distance = (target.CenterLocation - Dest).Length;
			var rawDamage = Weapon.Damage * (float)Math.Exp(-distance / Warhead.Spread);
			var multiplier = Warhead.EffectivenessAgainst(target.unitInfo.Armor);

			return rawDamage * multiplier;
		}
	}
}
