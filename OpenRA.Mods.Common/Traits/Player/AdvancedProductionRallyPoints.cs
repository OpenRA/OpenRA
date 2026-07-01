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

using System.Collections.Generic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.Player)]
	public class AdvancedProductionRallyPointsInfo : TraitInfo
	{
		public override object Create(ActorInitializer init) { return new AdvancedProductionRallyPoints(); }
	}

	public class AdvancedProductionRallyPoints : IResolveOrder
	{
		public const string SetOrder = "SetAdvancedProductionRallyPoint";

		readonly Dictionary<string, CPos> rallyPointsByUnit = [];

		public bool HasRallyPoint(string itemName)
		{
			return !string.IsNullOrEmpty(itemName) && rallyPointsByUnit.ContainsKey(itemName);
		}

		public bool TryGetRallyPoint(string itemName, out CPos cell)
		{
			cell = default;
			return !string.IsNullOrEmpty(itemName) && rallyPointsByUnit.TryGetValue(itemName, out cell);
		}

		public CPos[] GetRallyPath(string itemName)
		{
			return TryGetRallyPoint(itemName, out var cell) ? [cell] : null;
		}

		public void ResolveOrder(Actor self, Order order)
		{
			if (order.OrderString != SetOrder || string.IsNullOrEmpty(order.TargetString) || order.Target.Type != TargetType.Terrain)
				return;

			var cell = self.World.Map.CellContaining(order.Target.CenterPosition);
			if (!self.World.Map.Contains(cell))
				return;

			rallyPointsByUnit[order.TargetString] = cell;
		}
	}
}
