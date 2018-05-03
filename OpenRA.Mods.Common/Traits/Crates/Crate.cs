#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
	// Type tag for collector type bits
	public class CollectorType { }

	class CrateInfo : ITraitInfo, IPositionableInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Length of time (in seconds) until the crate gets removed automatically. " +
			"A value of zero disables auto-removal.")]
		public readonly int Lifetime = 0;

		[Desc("Allowed to land on.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		[Desc("Define actors that can collect crates by setting one of these into the Collects field from the CrateCollector trait.")]
		public readonly BitSet<CollectorType> ValidCollectorTypes = new BitSet<CollectorType>("crate-collector");

		[Desc("Set this to 'true' to allow crates to spawn on cells occupied by other actors. " +
			"Note that this might make the crate unreachable if spawned on immobile, unremovable actors.")]
		public readonly bool IgnoreActorsOnSpawning = false;

		public object Create(ActorInitializer init) { return new Crate(init, this); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new ReadOnlyDictionary<CPos, SubCell>();
		}

		bool IOccupySpaceInfo.SharesCell { get { return false; } }

		public bool CanEnterCell(World world, Actor self, CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return GetAvailableSubCell(world, cell, ignoreActor, checkTransientActors) != SubCell.Invalid;
		}

		public bool CanExistInCell(World world, CPos cell)
		{
			if (!world.Map.Contains(cell))
				return false;

			var type = world.Map.GetTerrainInfo(cell).Type;
			if (!TerrainTypes.Contains(type))
				return false;

			return true;
		}

		public SubCell GetAvailableSubCell(World world, CPos cell, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			if (!CanExistInCell(world, cell))
				return SubCell.Invalid;

			if (IgnoreActorsOnSpawning)
				return SubCell.FullCell;

			if (world.WorldActor.Trait<BuildingInfluence>().GetBuildingAt(cell) != null)
				return SubCell.Invalid;

			if (!checkTransientActors)
				return SubCell.FullCell;

			return !world.ActorMap.GetActorsAt(cell).Any(x => x != ignoreActor)
				? SubCell.FullCell : SubCell.Invalid;
		}
	}

	class Crate : ITick, IPositionable, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		static readonly Pair<CPos, SubCell>[] NoCells = { };

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

		void CheckForCollectors(Actor self)
		{
			// Crate can only be collected if it is not in the air.
			if (!self.IsAtGroundLevel())
				return;

			// Check whether any other (ground) actors are in this cell.
			var sameCellActors = self.World.ActorMap.GetActorsAt(self.Location).Where(a => a != self);

			// HACK: Currently needed to find aircraft actors.
			// TODO: Remove this once GetActorsAt supports aircraft.
			sameCellActors.Concat(self.World.FindActorsInCircle(self.CenterPosition, new WDist(724))
				.Where(a => a != self && a.Location == self.Location && !sameCellActors.Contains(a)));

			if (!sameCellActors.Any())
				return;

			var collector = sameCellActors.FirstOrDefault(a =>
			{
				if (!a.IsAtGroundLevel())
					return false;

				var crateCollectorTraitInfo = a.Info.TraitInfoOrDefault<CrateCollectorInfo>();
				if (crateCollectorTraitInfo == null)
					return false;

				// Make sure that the actor can collect this crate type
				return crateCollectorTraitInfo.All || crateCollectorTraitInfo.CollectorTypes.Overlaps(info.ValidCollectorTypes);
			});

			if (collector != null)
				OnCollectInner(collector);
		}

		void OnCollectInner(Actor collector)
		{
			if (collected)
				return;

			var crateActions = self.TraitsImplementing<CrateAction>();

			self.Dispose();
			collected = true;

			if (crateActions.Any())
			{
				var shares = crateActions.Select(a => Pair.New(a, a.GetSelectionSharesOuter(collector)));

				var totalShares = shares.Sum(a => a.Second);
				var n = self.World.SharedRandom.Next(totalShares);

				foreach (var s in shares)
				{
					if (n < s.Second)
					{
						s.First.Activate(collector);
						return;
					}

					n -= s.Second;
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (info.Lifetime != 0 && self.IsInWorld && ++ticks >= info.Lifetime * 25)
				self.Dispose();

			CheckForCollectors(self);
		}

		public CPos TopLeft { get { return Location; } }
		public Pair<CPos, SubCell>[] OccupiedCells() { return NoCells; }

		public WPos CenterPosition { get; private set; }

		// Sets the location (Location) and visual position (CenterPosition)
		public void SetPosition(Actor self, WPos pos)
		{
			var cell = self.World.Map.CellContaining(pos);
			SetLocation(self, cell);
			SetVisualPosition(self, self.World.Map.CenterOfCell(cell) + new WVec(WDist.Zero, WDist.Zero, self.World.Map.DistanceAboveTerrain(pos)));
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
			return info.GetAvailableSubCell(self.World, cell, ignoreActor, checkTransientActors);
		}

		public bool CanExistInCell(CPos cell) { return info.CanExistInCell(self.World, cell); }

		public bool CanEnterCell(CPos a, Actor ignoreActor = null, bool checkTransientActors = true)
		{
			return GetAvailableSubCell(a, SubCell.Any, ignoreActor, checkTransientActors) != SubCell.Invalid;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);

			var cs = self.World.WorldActor.TraitOrDefault<CrateSpawner>();
			if (cs != null)
				cs.IncrementCrates();
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);

			var cs = self.World.WorldActor.TraitOrDefault<CrateSpawner>();
			if (cs != null)
				cs.DecrementCrates();
		}
	}
}
