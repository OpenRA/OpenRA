#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	public class RevealsShroudInfo : ITraitInfo
	{
		public readonly WDist Range = WDist.Zero;

		[Desc("Possible values are CenterPosition (measure range from the center) and ",
			"Footprint (measure range from the footprint)")]
		public readonly VisibilityType Type = VisibilityType.Footprint;

		public virtual object Create(ActorInitializer init) { return new RevealsShroud(init.Self, this); }
	}

	public class RevealsShroud : ITick, ISync, INotifyAddedToWorld, INotifyRemovedFromWorld
	{
		static readonly PPos[] NoCells = { };

		readonly RevealsShroudInfo info;
		[Sync] CPos cachedLocation;
		[Sync] bool cachedDisabled;

		protected Action<Player, PPos[]> addCellsToPlayerShroud;
		protected Action<Player> removeCellsFromPlayerShroud;
		protected Func<bool> isDisabled;

		public RevealsShroud(Actor self, RevealsShroudInfo info)
		{
			this.info = info;

			addCellsToPlayerShroud = (p, uv) => p.Shroud.AddProjectedVisibility(self, uv);
			removeCellsFromPlayerShroud = p => p.Shroud.RemoveVisibility(self);
			isDisabled = () => false;
		}

		PPos[] ProjectedCells(Actor self)
		{
			var map = self.World.Map;
			var range = Range;
			if (range == WDist.Zero)
				return NoCells;

			if (info.Type == VisibilityType.Footprint)
				return self.OccupiesSpace.OccupiedCells()
					.SelectMany(kv => Shroud.ProjectedCellsInRange(map, kv.First, range))
					.Distinct().ToArray();

			return Shroud.ProjectedCellsInRange(map, self.CenterPosition, range)
				.ToArray();
		}

		public void Tick(Actor self)
		{
			if (!self.IsInWorld)
				return;

			var centerPosition = self.CenterPosition;
			var projectedPos = centerPosition - new WVec(0, centerPosition.Z, centerPosition.Z);
			var projectedLocation = self.World.Map.CellContaining(projectedPos);
			var disabled = isDisabled();

			if (cachedLocation == projectedLocation && cachedDisabled == disabled)
				return;

			cachedLocation = projectedLocation;
			cachedDisabled = disabled;

			var cells = ProjectedCells(self);
			foreach (var p in self.World.Players)
			{
				removeCellsFromPlayerShroud(p);
				addCellsToPlayerShroud(p, cells);
			}
		}

		public void AddedToWorld(Actor self)
		{
			var centerPosition = self.CenterPosition;
			var projectedPos = centerPosition - new WVec(0, centerPosition.Z, centerPosition.Z);
			cachedLocation = self.World.Map.CellContaining(projectedPos);
			cachedDisabled = isDisabled();
			var cells = ProjectedCells(self);
			foreach (var p in self.World.Players)
				addCellsToPlayerShroud(p, cells);
		}

		public void RemovedFromWorld(Actor self)
		{
			foreach (var p in self.World.Players)
				removeCellsFromPlayerShroud(p);
		}

		public WDist Range { get { return cachedDisabled ? WDist.Zero : info.Range; } }
	}
}
