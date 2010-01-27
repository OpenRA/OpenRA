using System.Collections.Generic;
using System.Linq;
using OpenRa.Effects;
using OpenRa.Traits;

/*
 * Crates left to implement:
Armor=10,ARMOR,2.0              ; armor of nearby objects increased (armor multiplier)
Cloak=0,STEALTH2                ; enable cloaking on nearby objects
Darkness=1,EMPULSE              ; cloak entire radar map
Explosion=5,NONE,500            ; high explosive baddie (damage per explosion)
Firepower=10,FPOWER,2.0         ; firepower of nearby objects increased (firepower multiplier)
HealBase=1,INVUN                ; all buildings to full strength
ICBM=1,MISSILE2                 ; nuke missile one time shot
Money=50,DOLLAR,2000            ; a chunk o' cash (maximum cash)
Napalm=5,NONE,600               ; fire explosion baddie (damage)
ParaBomb=3,PARABOX              ; para-bomb raid one time shot
Reveal=1,EARTH                  ; reveal entire radar map
Sonar=3,SONARBOX                ; one time sonar pulse
Squad=20,NONE                   ; squad of random infantry
Unit=20,NONE                    ; vehicle
Invulnerability=3,INVULBOX,1.0  ; invulnerability (duration in minutes)
TimeQuake=3,TQUAKE              ; time quake
*/

namespace OpenRa.Traits
{
	class CrateInfo : ITraitInfo
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
			self.World.UnitInfluence.Add(self, this);
		}

		public void OnCrush(Actor crusher)
		{
			// TODO: Pick one randomly
			self.traits.WithInterface<ICrateAction>().First().Activate(crusher);
			self.World.AddFrameEndTask(w =>	w.Remove(self));
		}

		public bool IsPathableCrush(UnitMovementType umt, Player player)
		{
			return true;
		}

		public bool IsCrushableBy(UnitMovementType umt, Player player)
		{
			return true;
		}

		public IEnumerable<int2> OccupiedCells() { yield return self.Location; }
		
		public void Tick(Actor self)
		{
			if (++ticks >= self.Info.Traits.Get<CrateInfo>().Lifetime * 25)
			{
				self.World.AddFrameEndTask(w =>	w.Remove(self));
			}
		}
	}
}
