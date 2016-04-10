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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	class AircraftMoveOrderTargeter : IOrderTargeter
	{
		public string OrderID { get { return "Move"; } }
		public int OrderPriority { get { return 4; } }
		public bool TargetOverridesSelection(TargetModifiers modifiers)
		{
			return modifiers.HasModifier(TargetModifiers.ForceMove);
		}

		readonly AircraftInfo info;

		public AircraftMoveOrderTargeter(AircraftInfo info) { this.info = info; }

		public bool CanTarget(Actor self, Target target, List<Actor> othersAtTarget, ref TargetModifiers modifiers, ref string cursor)
		{
			if (target.Type != TargetType.Terrain)
				return false;

			var location = self.World.Map.CellContaining(target.CenterPosition);
			var explored = self.Owner.Shroud.IsExplored(location);
			cursor = self.World.Map.Contains(location) ?
				(self.World.Map.GetTerrainInfo(location).CustomCursor ?? "move") :
				"move-blocked";

			IsQueued = modifiers.HasModifier(TargetModifiers.ForceQueue);

			if (!explored && !info.MoveIntoShroud)
				cursor = "move-blocked";

			return true;
		}

		public bool IsQueued { get; protected set; }
	}
}
