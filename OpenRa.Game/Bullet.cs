using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRa.Game.GameRules;

namespace OpenRa.Game
{
	class Bullet
	{
		readonly Player Owner;
		readonly Actor FiredBy;
		readonly WeaponInfo Weapon;
		readonly ProjectileInfo Projectile;
		readonly WarheadInfo Warhead;
		readonly int2 Src;
		readonly int2 Dest;

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
		}

		public void Tick(Game game, int dt) { /* todo */ }
	}
}
