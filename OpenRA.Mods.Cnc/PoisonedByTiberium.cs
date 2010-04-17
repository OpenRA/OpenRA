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

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Cnc
{
	class PoisonedByTiberiumInfo : ITraitInfo
	{	
		public readonly string Weapon = "Tiberium";
		public readonly string[] Resources = { "Tiberium" };

		public object Create(Actor self) { return new PoisonedByTiberium(this); }
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
				var rl = self.World.WorldActor.traits.Get<ResourceLayer>();
				var r = rl.GetResource(self.Location);

				if (r != null && info.Resources.Contains(r.Name))
					Combat.DoImpacts(new ProjectileArgs
					{
						src = self.CenterLocation.ToInt2(),
						dest = self.CenterLocation.ToInt2(),
						srcAltitude = 0,
						destAltitude = 0,
						facing = 0,
						firedBy = self,
						target = self,
						weapon = Rules.Weapons[info.Weapon.ToLowerInvariant()]
					}, self.CenterLocation.ToInt2());

				poisonTicks = Rules.Weapons[info.Weapon.ToLowerInvariant()].ROF;
			}	
		}
	}
}
