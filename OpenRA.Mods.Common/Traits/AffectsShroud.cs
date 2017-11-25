#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public abstract class AffectsShroudInfo : ConditionalTraitInfo
	{
		public readonly WDist Range = WDist.Zero;

		[Desc("If >= 0, prevent cells that are this much higher than the actor from being revealed.")]
		public readonly int MaxHeightDelta = -1;

		[Desc("Possible values are CenterPosition (measure range from the center) and ",
			"Footprint (measure range from the footprint)")]
		public readonly VisibilityType Type = VisibilityType.Footprint;
	}

	public abstract class AffectsShroud : ConditionalTrait<AffectsShroudInfo>, ITick, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		static readonly PPos[] NoCells = { };

		[Sync] CPos cachedLocation;
		[Sync] bool cachedTraitDisabled;

		protected abstract void AddCellsToPlayerShroud(Actor self, Player player, PPos[] uv);
		protected abstract void RemoveCellsFromPlayerShroud(Actor self, Player player);

		public AffectsShroud(Actor self, AffectsShroudInfo info)
			: base(info) { }

		PPos[] ProjectedCells(Actor self)
		{
			var map = self.World.Map;
			var range = Range;
			if (range == WDist.Zero)
				return NoCells;

			if (Info.Type == VisibilityType.Footprint)
				return self.OccupiesSpace.OccupiedCells()
					.SelectMany(kv => Shroud.ProjectedCellsInRange(map, kv.First, range, Info.MaxHeightDelta))
					.Distinct().ToArray();

			var pos = self.CenterPosition;
			if (Info.Type == VisibilityType.GroundPosition)
				pos -= new WVec(WDist.Zero, WDist.Zero, self.World.Map.DistanceAboveTerrain(pos));

			return Shroud.ProjectedCellsInRange(map, pos, range, Info.MaxHeightDelta)
				.ToArray();
		}

		void ITick.Tick(Actor self)
		{
			if (!self.IsInWorld)
				return;

			var centerPosition = self.CenterPosition;
			var projectedPos = centerPosition - new WVec(0, centerPosition.Z, centerPosition.Z);
			var projectedLocation = self.World.Map.CellContaining(projectedPos);
			var traitDisabled = IsTraitDisabled;

			if (cachedLocation == projectedLocation && traitDisabled == cachedTraitDisabled)
				return;

			cachedLocation = projectedLocation;
			cachedTraitDisabled = traitDisabled;

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
			cachedTraitDisabled = IsTraitDisabled;
			var cells = ProjectedCells(self);

			foreach (var p in self.World.Players)
				AddCellsToPlayerShroud(self, p, cells);
		}

		void INotifyRemovedFromWorld.RemovedFromWorld(Actor self)
		{
			foreach (var p in self.World.Players)
				RemoveCellsFromPlayerShroud(self, p);
		}

		public WDist Range { get { return cachedTraitDisabled ? WDist.Zero : Info.Range; } }
	}
}
