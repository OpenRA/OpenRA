#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
	public class RepairOrderGenerator : OrderGenerator
	{
		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, mi);
		}

		static IEnumerable<Order> OrderInner(World world, MouseInput mi)
		{
			if (mi.Button != MouseButton.Left)
				yield break;

			var underCursor = world.ScreenMap.ActorsAtMouse(mi)
				.Select(a => a.Actor)
				.FirstOrDefault(a => a.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && !world.FogObscures(a));

			if (underCursor == null)
				yield break;

			if (underCursor.GetDamageState() == DamageState.Undamaged)
				yield break;

			// Repair a building.
			if (underCursor.Info.HasTraitInfo<RepairableBuildingInfo>())
				yield return new Order("RepairBuilding", world.LocalPlayer.PlayerActor, Target.FromActor(underCursor), false);

			// Don't command allied units
			if (underCursor.Owner != world.LocalPlayer)
				yield break;

			Actor repairBuilding = null;
			var orderId = "Repair";

			// Test for generic Repairable (used on units).
			var repairable = underCursor.TraitOrDefault<Repairable>();
			if (repairable != null)
				repairBuilding = repairable.FindRepairBuilding(underCursor);
			else
			{
				var repairableNear = underCursor.TraitOrDefault<RepairableNear>();
				if (repairableNear != null)
				{
					orderId = "RepairNear";
					repairBuilding = repairableNear.FindRepairBuilding(underCursor);
				}
			}

			if (repairBuilding == null)
				yield break;

			yield return new Order(orderId, underCursor, Target.FromActor(repairBuilding), Target.FromActor(underCursor), mi.Modifiers.HasModifier(Modifiers.Shift));
		}

		protected override void Tick(World world)
		{
			if (world.LocalPlayer != null &&
				world.LocalPlayer.WinState != WinState.Undefined)
				world.CancelInputMode();
		}

		protected override IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAboveShroud(WorldRenderer wr, World world) { yield break; }
		protected override IEnumerable<IRenderable> RenderAnnotations(WorldRenderer wr, World world) { yield break; }

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, mi).Any()
				? "repair" : "repair-blocked";
		}
	}
}
