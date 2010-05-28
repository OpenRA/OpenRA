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

using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class LeavesHuskInfo : TraitInfo<LeavesHusk> { public readonly string HuskActor = null;	}

	class LeavesHusk : INotifyDamage
	{
		public void Damaged(Actor self, AttackInfo e)
		{
			if (e.DamageState == DamageState.Dead)
				self.World.AddFrameEndTask(w =>
					{
						var info = self.Info.Traits.Get<LeavesHuskInfo>();
						var husk = w.CreateActor(info.HuskActor, self.Location, self.Owner);
						husk.CenterLocation = self.CenterLocation;
						husk.traits.Get<Unit>().Altitude = self.traits.Get<Unit>().Altitude;
						husk.traits.Get<Unit>().Facing = self.traits.Get<Unit>().Facing;

						var turreted = self.traits.GetOrDefault<Turreted>();
						if (turreted != null)
							foreach (var p in husk.traits.WithInterface<ThrowsParticle>())
								p.InitialFacing = turreted.turretFacing;
					});
		}
	}
}
