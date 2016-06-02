#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	class CrateInfo : ITraitInfo, IPositionableInfo, IOccupySpaceInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Length of time (in seconds) until the crate gets removed automatically. " +
			"A value of zero disables auto-removal.")]
		public readonly int Lifetime = 0;

		[Desc("Allowed to land on.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		[Desc("Define actors that can collect crates by setting this into the Crushes field from the Mobile trait.")]
		public readonly string CrushClass = "crate";

		public object Create(ActorInitializer init) { return new Crate(init, this); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			var occupied = new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
			return new ReadOnlyDictionary<CPos, SubCell>(occupied);
		}

		bool IOccupySpaceInfo.SharesCell { get { return false; } }
	}

	class Crate : ITick, IPositionable, ICrushable, ISync,
		INotifyParachuteLanded, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyCrushed
	{
		readonly Actor self;
		readonly CrateInfo info;
		bool collected;

		[Sync] int ticks;
		[Sync] public CPos Location;

		public Crate(ActorInitializer init, CrateInfo info)
		{
			self = init.Self;
			this.info = info;

			if (init.Contains<LocationInit>())
				SetPosition(self, init.Get<LocationInit, CPos>());
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, HashSet<string> crushClasses) { }

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			// Crate can only be crushed if it is not in the air.
			if (!self.IsAtGroundLevel() || !crushClasses.Contains(info.CrushClass))
				return;

			OnCrushInner(crusher);
		}

		void INotifyParachuteLanded.OnLanded(Actor ignore)
		{
			// Check whether the crate landed on anything
			var landedOn = self.World.ActorMap.GetActorsAt(self.Location)
				.Where(a => a != self);

			if (!landedOn.Any())
				return;

			var collector = landedOn.FirstOrDefault(a =>
			{
				// Mobile is (currently) the only trait that supports crushing
				var mi = a.Info.TraitInfoOrDefault<MobileInfo>();
				if (mi == null)
					return false;

				// Make sure that the actor can collect this crate type
				// Crate can only be crushed if it is not in the air.
				return self.IsAtGroundLevel() && mi.Crushes.Contains(info.CrushClass);
			});

			// Destroy the crate if none of the units in the cell are valid collectors
			if (collector != null)
				OnCrushInner(collector);
			else
				self.Dispose();
		}

		void OnCrushInner(Actor crusher)
		{
			if (collected)
				return;

			var crateActions = self.TraitsImplementing<CrateAction>();

			self.Dispose();
			collected = true;

			if (crateActions.Any())
			{
				var shares = crateActions.Select(a => Pair.New(a, a.GetSelectionSharesOuter(crusher)));

				var totalShares = shares.Sum(a => a.Second);
				var n = self.World.SharedRandom.Next(totalShares);

				foreach (var s in shares)
				{
					if (n < s.Second)
					{
						s.First.Activate(crusher);
						return;
					}

					n -= s.Second;
				}
			}
		}

		public void Tick(Actor self)
		{
			if (info.Lifetime != 0 && self.IsInWorld && ++ticks >= info.Lifetime * 25)
				self.Dispose();
		}

		public CPos TopLeft { get { return Location; } }
		public IEnumerable<Pair<CPos, SubCell>> OccupiedCells() { return new[] { Pair.New(Location, SubCell.FullCell) }; }

		public WPos CenterPosition { get; private set; }

		// Sets the location (Location) and visual position (CenterPosition)
		public void SetPosition(Actor self, WPos pos)
		{
			var cell = self.World.Map.CellContaining(pos);
			SetLocation(self, cell);
			SetVisualPosition(self, self.World.Map.CenterOfCell(cell) + new WVec(0, 0, pos.Z));
		}

		// Sets the location (Location) and visual position (CenterPosition)
		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			SetLocation(self, cell, subCell);
			SetVisualPosition(self, self.World.Map.CenterOfCell(cell));
		}

		// Sets only the visual position (CenterPosition)
		public void SetVisualPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			self.World.UpdateMaps(self, this);
		}

		// Sets only the location (Location)
		void SetLocation(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			Location = cell;
			self.World.ActorMap.AddInfluence(self, this);
		}

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

			return !self.World.ActorMap.GetActorsAt(cell)
				.Any(x => x != ignoreActor)
				? SubCell.FullCell : SubCell.Invalid;
		}

		public bool CanEnterCell(CPos a, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return GetAvailableSubCell(a, SubCell.Any, ignoreActor, checkTransientActors) != SubCell.Invalid;
		}

		bool ICrushable.CrushableBy(Actor self, Actor crusher, HashSet<string> crushClasses)
		{
			// Crate can only be crushed if it is not in the air.
			return self.IsAtGroundLevel() && crushClasses.Contains(info.CrushClass);
		}

		public void AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);

			var cs = self.World.WorldActor.TraitOrDefault<CrateSpawner>();
			if (cs != null)
				cs.IncrementCrates();
		}

		public void RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);

			var cs = self.World.WorldActor.TraitOrDefault<CrateSpawner>();
			if (cs != null)
				cs.DecrementCrates();
		}
	}
}
