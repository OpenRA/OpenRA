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
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public class RepairOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, mi);
		}

		static IEnumerable<Order> OrderInner(World world, MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				yield break;

			var underCursor = world.ScreenMap.ActorsAt(mi)
				.FirstOrDefault(a => a.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && !world.FogObscures(a));

			if (underCursor == null)
				yield break;

			if (underCursor.GetDamageState() == DamageState.Undamaged)
				yield break;

			// Repair a building.
			if (underCursor.Info.HasTraitInfo<RepairableBuildingInfo>())
				yield return new Order("RepairBuilding", world.LocalPlayer.PlayerActor, false) { TargetActor = underCursor };

			// Don't command allied units
			if (underCursor.Owner != world.LocalPlayer)
				yield break;

			// Test for generic Repairable (used on units).
			var repairable = underCursor.TraitOrDefault<Repairable>();
			if (repairable == null)
				yield break;

			// Find a building to repair at.
			var repairBuilding = repairable.FindRepairBuilding(underCursor);
			if (repairBuilding == null)
				yield break;

			yield return new Order("Repair", underCursor, false) { TargetActor = repairBuilding };
		}

		public void Tick(World world)
		{
			if (world.LocalPlayer != null &&
				world.LocalPlayer.WinState != WinState.Undefined)
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }

		public string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, mi).Any()
				? "repair" : "repair-blocked";
		}
	}
}
