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

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.GameRules;

/*
 * Crates left to implement:
Cloak=0,STEALTH2                ; enable cloaking on nearby objects
Darkness=1,EMPULSE              ; cloak entire radar map
HealBase=1,INVUN                ; all buildings to full strength
ICBM=1,MISSILE2                 ; nuke missile one time shot
ParaBomb=3,PARABOX              ; para-bomb raid one time shot
Reveal=1,EARTH                  ; reveal entire radar map
Sonar=3,SONARBOX                ; one time sonar pulse
Squad=20,NONE                   ; squad of random infantry
Unit=20,NONE                    ; vehicle
Invulnerability=3,INVULBOX,1.0  ; invulnerability (duration in minutes)
TimeQuake=3,TQUAKE              ; time quake
*/

namespace OpenRA.Traits
{
	class CrateInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public readonly int Lifetime = 5; // Seconds
		public object Create(Actor self) { return new Crate(self); }
	}

	class Crate : ICrushable, IOccupySpace, ITick
	{
		readonly Actor self;
		int ticks;
		public Crate(Actor self)
		{
			this.self = self;
			self.World.WorldActor.traits.Get<UnitInfluence>().Add(self, this);

			if (self.World.GetTerrainType(self.Location) == TerrainType.Water)
				self.traits.Get<RenderSimple>().anim.PlayRepeating("water");
		}

		public void OnCrush(Actor crusher)
		{
			var shares = self.traits.WithInterface<CrateAction>().Select(a => Pair.New(a, a.GetSelectionShares(crusher)));
			var totalShares = shares.Sum(a => a.Second);
			var n = self.World.SharedRandom.Next(totalShares);

			self.World.AddFrameEndTask(w => w.Remove(self));
			foreach (var s in shares)
				if (n < s.Second)
				{
					s.First.Activate(crusher);
					return;
				}
				else
					n -= s.Second;
		}

		public bool IsPathableCrush(UnitMovementType umt, Player player) { return true; }
		public bool IsCrushableBy(UnitMovementType umt, Player player) { return true; }

		public IEnumerable<int2> OccupiedCells() { yield return self.Location; }
		
		public void Tick(Actor self)
		{
			if (++ticks >= self.Info.Traits.Get<CrateInfo>().Lifetime * 25)
				self.World.AddFrameEndTask(w =>	w.Remove(self));
		}
	}
}
