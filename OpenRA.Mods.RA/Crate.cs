#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.FileFormats;
using OpenRA.Traits;
using OpenRA.Mods.RA.Buildings;

/*
 * Crates left to implement:
HealBase=1,INVUN                ; all buildings to full strength
ICBM=1,MISSILE2                 ; nuke missile one time shot
Sonar=3,SONARBOX                ; one time sonar pulse
Squad=20,NONE                   ; squad of random infantry
Unit=20,NONE                    ; vehicle
Invulnerability=3,INVULBOX,1.0  ; invulnerability (duration in minutes)
TimeQuake=3,TQUAKE              ; time quake
*/

namespace OpenRA.Mods.RA
{
	class CrateInfo : ITraitInfo, Requires<RenderSimpleInfo>
	{
		public readonly int Lifetime = 5; // Seconds
		public readonly string[] TerrainTypes = { };
		public object Create(ActorInitializer init) { return new Crate(init, this); }
	}

	// ITeleportable is required for paradrop
	class Crate : ITick, IOccupySpace, ITeleportable, ICrushable, ISync
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
			if (init.Contains<LocationInit>())
			{
				this.Location = init.Get<LocationInit, int2>();
				PxPosition = Util.CenterOfCell(Location);
			}
			this.Info = info;
		}

		public void WarnCrush(Actor crusher) {}

		public void OnCrush(Actor crusher)
		{
			var shares = self.TraitsImplementing<CrateAction>().Select(
				a => Pair.New(a, a.GetSelectionSharesOuter(crusher)));
			var totalShares = shares.Sum(a => a.Second);
			var n = self.World.SharedRandom.Next(totalShares);

			self.Destroy();
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
			if( ++ticks >= Info.Lifetime * 25 )
				self.Destroy();
		}

		public int2 TopLeft { get { return Location; } }
		public IEnumerable<Pair<int2, SubCell>> OccupiedCells() { yield return Pair.New( Location, SubCell.FullCell); }

		public int2 PxPosition { get; private set; }

		public void SetPxPosition( Actor self, int2 px )
		{
			SetPosition( self, Util.CellContaining( px ) );
		}

		public void AdjustPxPosition(Actor self, int2 px) { SetPxPosition(self, px); }

		public bool CanEnterCell(int2 cell)
		{
			if (!self.World.Map.IsInMap(cell.X, cell.Y)) return false;
			var type = self.World.GetTerrainType(cell);
			if (!Info.TerrainTypes.Contains(type))
				return false;

			if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(cell) != null) return false;
			if (self.World.ActorMap.GetUnitsAt(cell).Any()) return false;

			return true;
		}

		public void SetPosition(Actor self, int2 cell)
		{
			if( self.IsInWorld )
				self.World.ActorMap.Remove(self, this);

			Location = cell;
			PxPosition = Util.CenterOfCell(cell);

			var seq = self.World.GetTerrainInfo(cell).IsWater ? "water" : "land";
			var rs = self.Trait<RenderSimple>();
			if (seq != rs.anim.CurrentSequence.Name)
				rs.anim.PlayRepeating(seq);

			if( self.IsInWorld )
				self.World.ActorMap.Add(self, this);
		}

		public bool CrushableBy(string[] crushClasses, Player owner)
		{
			return crushClasses.Contains("crate");
		}
	}
}
