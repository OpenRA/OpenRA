#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.RA;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class PoisonedByTiberiumInfo : ITraitInfo
	{	
		[WeaponReference]
		public readonly string Weapon = "Tiberium";
		public readonly string[] Resources = { "Tiberium" };

		public object Create(ActorInitializer init) { return new PoisonedByTiberium(this); }
	}

	class PoisonedByTiberium : ITick
	{
		PoisonedByTiberiumInfo info;
		[Sync] int poisonTicks;

		public PoisonedByTiberium(PoisonedByTiberiumInfo info) { this.info = info; }

		public void Tick(Actor self)
		{
			if (--poisonTicks <= 0)
			{
				var rl = self.World.WorldActor.Trait<ResourceLayer>();
				var r = rl.GetResource(self.Location);

				if (r != null && info.Resources.Contains(r.info.Name))
					Combat.DoImpacts(new ProjectileArgs
					{
						src = self.CenterLocation.ToInt2(),
						dest = self.CenterLocation.ToInt2(),
						srcAltitude = 0,
						destAltitude = 0,
						facing = 0,
						firedBy = self,
						target = Target.FromActor(self),
						weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()]
					});

				poisonTicks = Rules.Weapons[info.Weapon.ToLowerInvariant()].ROF;
			}	
		}
	}
}
