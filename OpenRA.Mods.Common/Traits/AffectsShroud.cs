#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class AffectsShroudInfo : ConditionalTraitInfo
	{
		public readonly WDist MinRange = WDist.Zero;

		public readonly WDist Range = WDist.Zero;

		[Desc("If >= 0, prevent cells that are this much higher than the actor from being revealed.")]
		public readonly int MaxHeightDelta = -1;

		[Desc("If > 0, force visibility to be recalculated if the unit moves within a cell by more than this distance.")]
		public readonly WDist MoveRecalculationThreshold = new WDist(256);

		[Desc("Possible values are CenterPosition (measure range from the center) and ",
			"Footprint (measure range from the footprint)")]
		public readonly VisibilityType Type = VisibilityType.Footprint;
	}

	public abstract class AffectsShroud : ConditionalTrait<AffectsShroudInfo>, ISync, INotifyAddedToWorld,
		INotifyRemovedFromWorld, INotifyMoving, INotifyCenterPositionChanged, ITick
	{
		static readonly PPos[] NoCells = Array.Empty<PPos>();

		readonly HashSet<PPos> footprint;

		[Sync]
		CPos cachedLocation;

		[Sync]
		WDist cachedRange;

		[Sync]
		protected bool CachedTraitDisabled { get; private set; }

		WPos cachedPos;

		protected abstract void AddCellsToPlayerShroud(Actor self, Player player, PPos[] uv);
		protected abstract void RemoveCellsFromPlayerShroud(Actor self, Player player);

		public AffectsShroud(AffectsShroudInfo info)
			: base(info)
		{
			if (Info.Type == VisibilityType.Footprint)
				footprint = new HashSet<PPos>();
		}

		PPos[] ProjectedCells(Actor self)
		{
			var map = self.World.Map;
			var minRange = Info.MinRange;
			var maxRange = Range;
			if (maxRange <= minRange)
				return NoCells;

			if (Info.Type == VisibilityType.Footprint)
			{
				// PERF: Reuse collection to avoid allocations.
				footprint.UnionWith(self.OccupiesSpace.OccupiedCells()
					.SelectMany(kv => Shroud.ProjectedCellsInRange(map, map.CenterOfCell(kv.Cell), minRange, maxRange, Info.MaxHeightDelta)));
				var cells = footprint.ToArray();
				footprint.Clear();
				return cells;
			}

			var pos = self.CenterPosition;
			if (Info.Type == VisibilityType.GroundPosition)
				pos -= new WVec(WDist.Zero, WDist.Zero, self.World.Map.DistanceAboveTerrain(pos));

			return Shroud.ProjectedCellsInRange(map, pos, minRange, maxRange, Info.MaxHeightDelta)
				.ToArray();
		}

		void INotifyCenterPositionChanged.CenterPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			if (!self.IsInWorld)
				return;

			var centerPosition = self.CenterPosition;
			var projectedPos = centerPosition - new WVec(0, centerPosition.Z, centerPosition.Z);
			var projectedLocation = self.World.Map.CellContaining(projectedPos);
			var pos = self.CenterPosition;

			var dirty = Info.MoveRecalculationThreshold.Length > 0 && (pos - cachedPos).LengthSquared > Info.MoveRecalculationThreshold.LengthSquared;
			if (!dirty && cachedLocation == projectedLocation)
				return;

			cachedLocation = projectedLocation;
			cachedPos = pos;

			UpdateShroudCells(self);
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld)
				return;

			var traitDisabled = IsTraitDisabled;
			var range = Range;

			if (cachedRange == range && traitDisabled == CachedTraitDisabled)
				return;

			cachedRange = range;
			CachedTraitDisabled = traitDisabled;

			UpdateShroudCells(self);
		}

		void UpdateShroudCells(Actor self)
		{
			var cells = ProjectedCells(self);
			foreach (var p in self.World.Players)
			{
				RemoveCellsFromPlayerShroud(self, p);
				AddCellsToPlayerShroud(self, p, cells);
			}
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var centerPosition = self.CenterPosition;
			var projectedPos = centerPosition - new WVec(0, centerPosition.Z, centerPosition.Z);
			cachedLocation = self.World.Map.CellContaining(projectedPos);
			cachedPos = centerPosition;
			CachedTraitDisabled = IsTraitDisabled;
			var cells = ProjectedCells(self);

			foreach (var p in self.World.Players)
				AddCellsToPlayerShroud(self, p, cells);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			foreach (var p in self.World.Players)
				RemoveCellsFromPlayerShroud(self, p);
		}

		public virtual WDist Range => CachedTraitDisabled ? WDist.Zero : Info.Range;

		void INotifyMoving.MovementTypeChanged(Actor self, MovementType type)
		{
			// Recalculate the visibility at our final stop position
			if (type == MovementType.None && self.IsInWorld)
			{
				var centerPosition = self.CenterPosition;
				var projectedPos = centerPosition - new WVec(0, centerPosition.Z, centerPosition.Z);
				var projectedLocation = self.World.Map.CellContaining(projectedPos);
				var pos = self.CenterPosition;

				cachedLocation = projectedLocation;
				cachedPos = pos;

				UpdateShroudCells(self);
			}
		}
	}
}
