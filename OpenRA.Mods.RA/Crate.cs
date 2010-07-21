#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
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
		public readonly string[] TerrainTypes = {};
		public object Create(ActorInitializer init) { return new Crate(init, this); }
	}

	// IMove is required for paradrop
	class Crate : ITick, IOccupySpace, IMove
	{
		readonly Actor self;
		[Sync]
		int ticks;

		[Sync]
		public int2 Location;

		CrateInfo Info;
		public Crate(ActorInitializer init, CrateInfo info)
		{
			this.self = init.self;
			this.Location = init.location;
			this.Info = info;
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
			var cell = Util.CellContaining(self.CenterLocation);
			var collector = self.World.WorldActor.traits.Get<UnitInfluence>().GetUnitsAt(cell).FirstOrDefault();
			if (collector != null)
			{
				OnCollected(collector);
				return;
			}

			if (++ticks >= self.Info.Traits.Get<CrateInfo>().Lifetime * 25)
				self.World.AddFrameEndTask(w =>	w.Remove(self));

			var seq = self.World.GetTerrainInfo(cell).IsWater ? "water" : "idle";
			if (seq != self.traits.Get<RenderSimple>().anim.CurrentSequence.Name)
				self.traits.Get<RenderSimple>().anim.PlayRepeating(seq);
		}
		
		public int2 TopLeft	{get { return Location; }}
		int2[] noCells = new int2[] { };
		public IEnumerable<int2> OccupiedCells() { return noCells; }

		public bool CanEnterCell(int2 cell) { return MovementCostForCell(self, cell) < float.PositiveInfinity; }

		public float MovementCostForCell(Actor self, int2 cell)
		{
			if (!self.World.Map.IsInMap(cell.X,cell.Y))
				return float.PositiveInfinity;
			
			var type = self.World.GetTerrainType(cell);
			return Info.TerrainTypes.Contains(type) ? 0f : float.PositiveInfinity;
		}
		
		public float MovementSpeedForCell(Actor self, int2 cell) { return 1; }
		public IEnumerable<float2> GetCurrentPath(Actor self) { return new float2[] {}; }

		public void SetPosition(Actor self, int2 cell)
		{
			Location = cell;
			self.CenterLocation = Util.CenterOfCell(cell);
		}
	}
}
