#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CarpetBombInfo : ITraitInfo
	{
		[WeaponReference]
		public readonly string Weapon = null;
		public readonly int Range = 3;

		public object Create(ActorInitializer init) { return new CarpetBomb(this); }
	}

	// TODO: maybe integrate this better with the normal weapons system?
	class CarpetBomb : ITick, ISync
	{
		CarpetBombInfo info;
		Target target;

		[Sync] int dropDelay;
		[Sync] WRange range;

		public CarpetBomb(CarpetBombInfo info)
		{
			this.info = info;

			// TODO: Push this conversion into the yaml
			range = WRange.FromCells(info.Range);
		}

		public void SetTarget(CPos targetCell) { target = Target.FromCell(targetCell); }

		public void Tick(Actor self)
		{
			if (!target.IsInRange(self.CenterPosition, range))
				return;

			var limitedAmmo = self.TraitOrDefault<LimitedAmmo>();
			if (limitedAmmo != null && !limitedAmmo.HasAmmo())
				return;

			if (--dropDelay <= 0)
			{
				var weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()];
				dropDelay = weapon.ROF;

				var args = new ProjectileArgs
				{
					srcAltitude = self.Trait<IOccupySpace>().Altitude,
					destAltitude = 0,
					src = self.CenterLocation,
					dest = self.CenterLocation,
					facing = self.Trait<IFacing>().Facing,
					firedBy = self,
					weapon = weapon
				};

				self.World.Add(args.weapon.Projectile.Create(args));

				if (args.weapon.Report != null && args.weapon.Report.Any())
					Sound.Play(args.weapon.Report.Random(self.World.SharedRandom), self.CenterPosition);
			}
		}
	}
}
