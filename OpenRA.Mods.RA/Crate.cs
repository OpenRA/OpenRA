#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Mods.RA.Move;
using OpenRA.Mods.RA.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	class CrateInfo : ITraitInfo, IOccupySpaceInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Seconds")]
		public readonly int Lifetime = 5;

		[Desc("Allowed to land on.")]
		public readonly string[] TerrainTypes = { };

		[Desc("Define actors that can collect crates by setting this into the Crushes field from the Mobile trait.")]
		public readonly string CrushClass = "crate";

		public object Create(ActorInitializer init) { return new Crate(init, this); }
	}

	class Crate : ITick, IPositionable, ICrushable, ISync, INotifyParachuteLanded, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		readonly Actor self;
		readonly CrateInfo info;
		bool collected;

		[Sync] int ticks;
		[Sync] public CPos Location;

		public Crate(ActorInitializer init, CrateInfo info)
		{
			this.self = init.self;
			this.info = info;

			if (init.Contains<LocationInit>())
				SetPosition(self, init.Get<LocationInit, CPos>());
		}

		public void WarnCrush(Actor crusher) { }

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
				if (landedOn.HasTrait<MobileInfo>() &&
					CrushableBy(landedOn.TraitOrDefault<MobileInfo>().Crushes, landedOn.Owner))
						OnCrush(landedOn);
				else
					self.Destroy();
		}

		public void Tick(Actor self)
		{
			if (++ticks >= info.Lifetime * 25)
				self.Destroy();
		}

		public CPos TopLeft { get { return Location; } }
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { yield return Pair.New(Location, SubCell.FullCell); }

		public WPos CenterPosition { get; private set; }
		public void SetPosition(Actor self, WPos pos) { SetPosition(self, self.World.Map.CellContaining(pos)); }
		public void SetVisualPosition(Actor self, WPos pos) { SetPosition(self, self.World.Map.CellContaining(pos)); }

		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return self.Location == location && ticks + 1 == info.Lifetime * 25; }
		public SubCell GetValidSubCell(SubCell preferred = SubCell.Any) { return SubCell.FullCell; }
		public SubCell GetAvailableSubCell(CPos cell, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			if (!self.World.Map.Contains(cell))
				return SubCell.Invalid;

			var type = self.World.Map.GetTerrainInfo(cell).Type;
			if (!info.TerrainTypes.Contains(type))
				return SubCell.Invalid;

			if (self.World.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(cell) != null)
				return SubCell.Invalid;

			if (!checkTransientActors)
				return SubCell.FullCell;

			return !self.World.ActorMap.GetUnitsAt(cell)
				.Where(x => x != ignoreActor)
				.Any() ? SubCell.FullCell : SubCell.Invalid;
		}

		public bool CanEnterCell(CPos a, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return GetAvailableSubCell(a, SubCell.Any, ignoreActor, checkTransientActors) != SubCell.Invalid;
		}

		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			Location = cell;
			CenterPosition = self.World.Map.CenterOfCell(cell);

			if (self.IsInWorld)
			{
				self.World.ActorMap.UpdatePosition(self, this);
				self.World.ScreenMap.Update(self);
			}
		}

		public bool CrushableBy(string[] crushClasses, Player owner)
		{
			return crushClasses.Contains(info.CrushClass);
		}

		public void AddedToWorld(Actor self)
		{
			self.World.ActorMap.AddInfluence(self, this);
			self.World.ActorMap.AddPosition(self, this);
			self.World.ScreenMap.Add(self);

			var cs = self.World.WorldActor.TraitOrDefault<CrateSpawner>();
			if (cs != null)
				cs.IncrementCrates();
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			self.World.ActorMap.RemovePosition(self, this);
			self.World.ScreenMap.Remove(self);

			var cs = self.World.WorldActor.TraitOrDefault<CrateSpawner>();
			if (cs != null)
				cs.DecrementCrates();
		}
	}
}
