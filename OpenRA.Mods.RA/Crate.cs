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

namespace OpenRA.Mods.RA
{
	class CrateInfo : ITraitInfo, Requires<RenderSpritesInfo>
	{
		public readonly int Lifetime = 5; // Seconds
		public readonly string[] TerrainTypes = { };
		public object Create(ActorInitializer init) { return new Crate(init, this); }
	}

	// ITeleportable is required for paradrop
	class Crate : ITick, ITeleportable, ICrushable, ISync, INotifyParachuteLanded
	{
		readonly Actor self;
		[Sync] int ticks;
		[Sync] public CPos Location;
		CrateInfo Info;
		bool collected;

		public Crate(ActorInitializer init, CrateInfo info)
		{
			this.self = init.self;
			if (init.Contains<LocationInit>())
			{
				this.Location = init.Get<LocationInit, CPos>();
				PxPosition = Util.CenterOfCell(Location);
			}
			this.Info = info;
		}

		public void WarnCrush(Actor crusher) {}

		public void OnCrush(Actor crusher)
		{
			if (collected) return;

			var shares = self.TraitsImplementing<CrateAction>().Select(
				a => Pair.New(a, a.GetSelectionSharesOuter(crusher)));
			var totalShares = shares.Sum(a => a.Second);
			var n = self.World.SharedRandom.Next(totalShares);

			self.Destroy();
			collected = true;

			foreach (var s in shares)
				if (n < s.Second)
				{
					s.First.Activate(crusher);
					return;
				}
				else
					n -= s.Second;
		}

		public void OnLanded()
		{
			var landedOn = self.World.ActorMap.GetUnitsAt(self.Location)
				.FirstOrDefault(a => a != self);

			if (landedOn != null)
				OnCrush(landedOn);
		}

		public void Tick(Actor self)
		{
			if( ++ticks >= Info.Lifetime * 25 )
				self.Destroy();
		}

		public CPos TopLeft { get { return Location; } }
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { yield return Pair.New( Location, SubCell.FullCell); }

		public PPos PxPosition { get; private set; }
		public int Altitude { get { return 0; } set { } }

		public void SetPxPosition(Actor self, PPos px)
		{
			SetPosition( self, px.ToCPos() );
		}

		public void AdjustPxPosition(Actor self, PPos px) { SetPxPosition(self, px); }

		public bool CanEnterCell(CPos cell)
		{
			if (!self.World.Map.IsInMap(cell.X, cell.Y)) return false;
			var type = self.World.GetTerrainType(cell);
			if (!Info.TerrainTypes.Contains(type))
				return false;

			if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(cell) != null) return false;
			if (self.World.ActorMap.GetUnitsAt(cell).Any()) return false;

			return true;
		}

		public void SetPosition(Actor self, CPos cell)
		{
			if( self.IsInWorld )
				self.World.ActorMap.Remove(self, this);

			Location = cell;
			PxPosition = Util.CenterOfCell(cell);

			var seq = self.World.GetTerrainInfo(cell).IsWater ? "water" : "land";
			var rs = self.Trait<RenderSprites>();
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
