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
	class CrateInfo : ITraitInfo, ITraitPrerequisite<RenderSimpleInfo>
	{
		public readonly int Lifetime = 5; // Seconds
		public readonly string[] TerrainTypes = { };
		public object Create(ActorInitializer init) { return new Crate(init, this); }
	}

	// ITeleportable is required for paradrop
	class Crate : ITick, IOccupySpace, ITeleportable, ICrushable
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
			if( ++ticks >= self.Info.Traits.Get<CrateInfo>().Lifetime * 25 )
				self.Destroy();
		}

		public int2 TopLeft { get { return Location; } }
		public IEnumerable<int2> OccupiedCells() { return new int2[] { Location }; }

		public int2 PxPosition { get; private set; }

		public void SetPxPosition( Actor self, int2 px )
		{
			SetPosition( self, Util.CellContaining( px ) );
		}

		public bool CanEnterCell(int2 cell)
		{
			if (!self.World.Map.IsInMap(cell.X, cell.Y)) return false;
			var type = self.World.GetTerrainType(cell);
			return Info.TerrainTypes.Contains(type);
		}

		public void SetPosition(Actor self, int2 cell)
		{
			var uim = self.World.WorldActor.Trait<UnitInfluence>();

			if( self.IsInWorld )
				uim.Remove(self, this);

			Location = cell;
			PxPosition = Util.CenterOfCell(cell);

			var seq = self.World.GetTerrainInfo(cell).IsWater ? "water" : "idle";
			if (seq != self.Trait<RenderSimple>().anim.CurrentSequence.Name)
				self.Trait<RenderSimple>().anim.PlayRepeating(seq);

			if( self.IsInWorld )
				uim.Add(self, this);
		}

		public IEnumerable<string> CrushClasses { get { yield return "crate"; } }
	}
}
