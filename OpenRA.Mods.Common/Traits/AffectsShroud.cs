#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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

		[Desc("If > 0, force visibility to be recalculated when a unit moves by more than this distance. " +
			"Applies to " + nameof(VisibilityType.CenterPosition) + " and " + nameof(VisibilityType.GroundPosition) + " types.")]
		public readonly WDist MoveRecalculationThreshold = new(512);

		[Desc("Possible values are " +
			nameof(VisibilityType.CenterPosition) + " (measure range from the center)," +
			nameof(VisibilityType.GroundPosition) + " (measure range from the ground level under center) and " +
			nameof(VisibilityType.Footprint) + " (measure range from the footprint)")]
		public readonly VisibilityType Type = VisibilityType.Footprint;
	}

	public abstract class AffectsShroud : ConditionalTrait<AffectsShroudInfo>, ISync, INotifyAddedToWorld,
		INotifyRemovedFromWorld, INotifyCenterPositionChanged, ITick
	{
		static readonly PPos[] NoCells = Array.Empty<PPos>();

		readonly HashSet<PPos> footprint;

		[Sync]
		WDist cachedRange;

		InputPositions cachedInput;

		protected abstract void AddCellsToPlayerShroud(Actor self, Player player, PPos[] uv);
		protected abstract void RemoveCellsFromPlayerShroud(Actor self, Player player);

		protected AffectsShroud(AffectsShroudInfo info)
			: base(info)
		{
			if (Info.Type == VisibilityType.Footprint)
				footprint = new HashSet<PPos>();
		}

		InputPositions ProjectedCellsInput(Actor self)
		{
			(CPos Cell, SubCell SubCell)[] cells = null;
			WPos position = default;

			if (Info.Type == VisibilityType.Footprint)
			{
				cells = self.OccupiesSpace.OccupiedCells();
			}
			else
			{
				position = self.CenterPosition;

				// Don't recalculate shroud for every tiny position change. Quantize the position.
				// Avoids us updating the shroud too much - until the position has changed a sufficient amount.
				var l = Info.MoveRecalculationThreshold.Length;
				if (l > 0)
				{
					var l2 = l / 2; // Put us in the middle of the region, half way along.
					position = new WPos(position.X / l * l + l2, position.Y / l * l + l2, position.Z / l * l + l2);
				}
			}

			return new InputPositions(cells, position);
		}

		PPos[] ProjectedCells(Actor self, InputPositions input)
		{
			var map = self.World.Map;
			var minRange = Info.MinRange;
			var maxRange = Range;
			if (maxRange <= minRange)
				return NoCells;

			if (Info.Type == VisibilityType.Footprint)
			{
				if (input.FootprintOccupiedCells.Length == 1)
				{
					var cellPosition = map.CenterOfCell(input.FootprintOccupiedCells[0].Cell);
					return Shroud.ProjectedCellsInRange(map, cellPosition, minRange, maxRange, Info.MaxHeightDelta)
						.ToArray();
				}

				// With multiple footprint cells we will produce overlapping shrouds, so we must de-duplicate the cells.
				// PERF: Reuse collection to avoid allocations.
				footprint.UnionWith(input.FootprintOccupiedCells
					.SelectMany(kv => Shroud.ProjectedCellsInRange(map, map.CenterOfCell(kv.Cell), minRange, maxRange, Info.MaxHeightDelta)));
				var cells = footprint.ToArray();
				footprint.Clear();
				return cells;
			}

			var position = input.CenterPosition;
			if (Info.Type == VisibilityType.GroundPosition)
				position -= new WVec(WDist.Zero, WDist.Zero, self.World.Map.DistanceAboveTerrain(position));

			return Shroud.ProjectedCellsInRange(map, position, minRange, maxRange, Info.MaxHeightDelta)
				.ToArray();
		}

		void INotifyCenterPositionChanged.CenterPositionChanged(Actor self, byte oldLayer, byte newLayer)
		{
			if (!self.IsInWorld)
				return;

			var input = ProjectedCellsInput(self);

			if (input == cachedInput)
				return;

			cachedInput = input;

			UpdateShroudCells(self, input);
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld)
				return;

			var range = Range;

			if (cachedRange == range)
				return;

			cachedRange = range;

			var input = ProjectedCellsInput(self);
			UpdateShroudCells(self, input);
		}

		void UpdateShroudCells(Actor self, InputPositions input)
		{
			var cells = ProjectedCells(self, input);
			foreach (var p in self.World.Players)
			{
				RemoveCellsFromPlayerShroud(self, p);
				AddCellsToPlayerShroud(self, p, cells);
			}
		}

		void INotifyAddedToWorld.AddedToWorld(Actor self)
		{
			var input = ProjectedCellsInput(self);
			var cells = ProjectedCells(self, input);

			foreach (var p in self.World.Players)
				AddCellsToPlayerShroud(self, p, cells);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			foreach (var p in self.World.Players)
				RemoveCellsFromPlayerShroud(self, p);
		}

		public virtual WDist Range => IsTraitDisabled ? WDist.Zero : Info.Range;

		readonly struct InputPositions : IEquatable<InputPositions>
		{
			public readonly (CPos Cell, SubCell SubCell)[] FootprintOccupiedCells { get; }
			public readonly WPos CenterPosition { get; }

			public InputPositions((CPos Cell, SubCell SubCell)[] footprintOccupiedCells, WPos centerPosition)
			{
				FootprintOccupiedCells = footprintOccupiedCells;
				CenterPosition = centerPosition;
			}

			public bool Equals(InputPositions other)
			{
				if (CenterPosition != other.CenterPosition) return false;
				if (FootprintOccupiedCells == null ^ other.FootprintOccupiedCells == null) return false;
				if (FootprintOccupiedCells == null && other.FootprintOccupiedCells == null) return true;
				return FootprintOccupiedCells.SequenceEqual(other.FootprintOccupiedCells);
			}

			public override int GetHashCode() =>
				HashCode.Combine(CenterPosition, FootprintOccupiedCells?.FirstOrDefault());

			public override bool Equals(object obj) => obj is InputPositions input && Equals(input);
			public static bool operator ==(InputPositions left, InputPositions right) => left.Equals(right);
			public static bool operator !=(InputPositions left, InputPositions right) => !(left == right);
		}
	}
}
