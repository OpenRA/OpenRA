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
using OpenRA.Traits;

/*
 * Crates left to implement:
Cloak=0,STEALTH2                ; enable cloaking on nearby objects
HealBase=1,INVUN                ; all buildings to full strength
ICBM=1,MISSILE2                 ; nuke missile one time shot
Reveal=1,EARTH                  ; reveal entire radar map
Sonar=3,SONARBOX                ; one time sonar pulse
Squad=20,NONE                   ; squad of random infantry
Unit=20,NONE                    ; vehicle
Invulnerability=3,INVULBOX,1.0  ; invulnerability (duration in minutes)
TimeQuake=3,TQUAKE              ; time quake
*/

namespace OpenRA.Mods.RA
{
	class CrateInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public readonly int Lifetime = 5; // Seconds
		public object Create(ActorInitializer init) { return new Crate(init); }
	}

	// IMove is required for paradrop
	class Crate : ITick, IOccupySpace, IMove
	{
		readonly Actor self;
		[Sync]
		int ticks;

		[Sync]
		public int2 Location;

		public Crate(ActorInitializer init)
		{
			this.self = init.self;
			this.Location = init.location;
		}

		public void OnCollected(Actor crusher)
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

		public void Tick(Actor self)
		{
			var cell = ((1/24f) * self.CenterLocation).ToInt2();
			var collector = self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(cell).FirstOrDefault();
			if (collector != null)
			{
				OnCollected(collector);
				return;
			}

			if (++ticks >= self.Info.Traits.Get<CrateInfo>().Lifetime * 25)
				self.World.AddFrameEndTask(w =>	w.Remove(self));

			var seq = self.World.GetTerrainType(cell) == "Water" ? "water" : "idle";
			if (seq != self.traits.Get<RenderSimple>().anim.CurrentSequence.Name)
				self.traits.Get<RenderSimple>().anim.PlayRepeating(seq);
		}
		
		public int2 TopLeft	{get { return Location; }}
		int2[] noCells = new int2[] { };
		public IEnumerable<int2> OccupiedCells() { return noCells; }
		
		public bool CanEnterCell(int2 location) { return true; }
		public float MovementCostForCell(Actor self, int2 cell) { return 0; }
		public float MovementSpeedForCell(Actor self, int2 cell) { return 1; }
		public IEnumerable<float2> GetCurrentPath(Actor self) { return new float2[] {}; }
		public void SetPosition(Actor self, int2 cell)
		{
			Location = cell;
			self.CenterLocation = Util.CenterOfCell(cell);
		}
	}
}
