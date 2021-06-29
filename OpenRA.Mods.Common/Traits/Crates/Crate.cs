#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Activities;
using OpenRA.Mods.Common.Traits.Render;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class CrateInfo : TraitInfo, IPositionableInfo, Requires<RenderSpritesInfo>
	{
		[Desc("Length of time (in ticks) until the crate gets removed automatically. " +
			"A value of zero disables auto-removal.")]
		public readonly int Duration = 0;

		[Desc("Allowed to land on.")]
		public readonly HashSet<string> TerrainTypes = new HashSet<string>();

		[Desc("Define actors that can collect crates by setting this into the Crushes field from the Mobile trait.")]
		public readonly string CrushClass = "crate";

		public override object Create(ActorInitializer init) { return new Crate(init, this); }

		public IReadOnlyDictionary<CPos, SubCell> OccupiedCells(ActorInfo info, CPos location, SubCell subCell = SubCell.Any)
		{
			return new Dictionary<CPos, SubCell>() { { location, SubCell.FullCell } };
		}

		bool IOccupySpaceInfo.SharesCell => false;

		public bool CanEnterCell(World world, Actor self, CPos cell, SubCell subCell = SubCell.FullCell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			// Since crates don't share cells and GetAvailableSubCell only returns SubCell.Full or SubCell.Invalid, we ignore the subCell parameter
			return GetAvailableSubCell(world, cell, ignoreActor, check) != SubCell.Invalid;
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

		public SubCell GetAvailableSubCell(World world, CPos cell, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			if (!CanExistInCell(world, cell))
				return SubCell.Invalid;

			if (check == BlockedByActor.None)
				return SubCell.FullCell;

			return !world.ActorMap.GetActorsAt(cell).Any(x => x != ignoreActor)
				? SubCell.FullCell : SubCell.Invalid;
		}
	}

	public class Crate : ITick, IPositionable, ICrushable, ISync, INotifyCreated,
		INotifyParachute, INotifyAddedToWorld, INotifyRemovedFromWorld, INotifyCrushed
	{
		readonly Actor self;
		readonly CrateInfo info;
		bool collected;
		INotifyCenterPositionChanged[] notifyCenterPositionChanged;

		[Sync]
		int ticks;

		[Sync]
		public CPos Location;

		public Crate(ActorInitializer init, CrateInfo info)
		{
			self = init.Self;
			this.info = info;

			var locationInit = init.GetOrDefault<LocationInit>();
			if (locationInit != null)
				SetPosition(self, locationInit.Value);
		}

		void INotifyCreated.Created(Actor self)
		{
			notifyCenterPositionChanged = self.TraitsImplementing<INotifyCenterPositionChanged>().ToArray();
		}

		void INotifyCrushed.WarnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses) { }

		void INotifyCrushed.OnCrush(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			// Crate can only be crushed if it is not in the air.
			if (!self.IsAtGroundLevel() || !crushClasses.Contains(info.CrushClass))
				return;

			OnCrushInner(crusher);
		}

		void INotifyParachute.OnParachute(Actor self) { }
		void INotifyParachute.OnLanded(Actor self)
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
				return self.IsAtGroundLevel() && mi.LocomotorInfo.Crushes.Contains(info.CrushClass);
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
				var shares = crateActions.Select(a => (Action: a, Shares: a.GetSelectionSharesOuter(crusher)));

				var totalShares = shares.Sum(a => a.Shares);
				var n = self.World.SharedRandom.Next(totalShares);

				foreach (var s in shares)
				{
					if (n < s.Shares)
					{
						s.Action.Activate(crusher);
						return;
					}

					n -= s.Shares;
				}
			}
		}

		void ITick.Tick(Actor self)
		{
			if (info.Duration != 0 && self.IsInWorld && ++ticks >= info.Duration)
				self.Dispose();
		}

		public CPos TopLeft => Location;
		public (CPos, SubCell)[] OccupiedCells() { return new[] { (Location, SubCell.FullCell) }; }

		public WPos CenterPosition { get; private set; }

		// Sets the location (Location) and position (CenterPosition)
		public void SetPosition(Actor self, WPos pos)
		{
			var cell = self.World.Map.CellContaining(pos);
			SetLocation(self, cell);
			SetCenterPosition(self, self.World.Map.CenterOfCell(cell) + new WVec(WDist.Zero, WDist.Zero, self.World.Map.DistanceAboveTerrain(pos)));
		}

		// Sets the location (Location) and position (CenterPosition)
		public void SetPosition(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			SetLocation(self, cell, subCell);
			SetCenterPosition(self, self.World.Map.CenterOfCell(cell));
		}

		// Sets only the CenterPosition
		public void SetCenterPosition(Actor self, WPos pos)
		{
			CenterPosition = pos;
			self.World.UpdateMaps(self, this);

			// This can be called from the constructor before notifyCenterPositionChanged is assigned.
			if (notifyCenterPositionChanged != null)
				foreach (var n in notifyCenterPositionChanged)
					n.CenterPositionChanged(self, 0, 0);
		}

		// Sets only the location (Location)
		void SetLocation(Actor self, CPos cell, SubCell subCell = SubCell.Any)
		{
			self.World.ActorMap.RemoveInfluence(self, this);
			Location = cell;
			self.World.ActorMap.AddInfluence(self, this);
		}

		public bool IsLeavingCell(CPos location, SubCell subCell = SubCell.Any) { return self.Location == location && ticks + 1 == info.Duration; }
		public SubCell GetValidSubCell(SubCell preferred = SubCell.Any) { return SubCell.FullCell; }
		public SubCell GetAvailableSubCell(CPos cell, SubCell preferredSubCell = SubCell.Any, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return info.GetAvailableSubCell(self.World, cell, ignoreActor, check);
		}

		public bool CanExistInCell(CPos cell) { return info.CanExistInCell(self.World, cell); }

		public bool CanEnterCell(CPos a, Actor ignoreActor = null, BlockedByActor check = BlockedByActor.All)
		{
			return GetAvailableSubCell(a, SubCell.Any, ignoreActor, check) != SubCell.Invalid;
		}

		bool ICrushable.CrushableBy(Actor self, Actor crusher, BitSet<CrushClass> crushClasses)
		{
			// Crate can only be crushed if it is not in the air.
			return self.IsAtGroundLevel() && crushClasses.Contains(info.CrushClass);
		}

		LongBitSet<PlayerBitMask> ICrushable.CrushableBy(Actor self, BitSet<CrushClass> crushClasses)
		{
			return self.IsAtGroundLevel() && crushClasses.Contains(info.CrushClass) ? self.World.AllPlayersMask : self.World.NoPlayersMask;
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			self.World.AddToMaps(self, this);

			self.World.WorldActor.TraitOrDefault<CrateSpawner>()?.IncrementCrates();

			if (self.World.Map.DistanceAboveTerrain(CenterPosition) > WDist.Zero && self.TraitOrDefault<Parachutable>() != null)
				self.QueueActivity(new Parachute(self));
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			self.World.RemoveFromMaps(self, this);

			self.World.WorldActor.TraitOrDefault<CrateSpawner>()?.DecrementCrates();
		}
	}
}
