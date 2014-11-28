#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;

namespace OpenRA.Mods.RA.Orders
{
	public class RepairOrderGenerator : IOrderGenerator
	{
		public IEnumerable<Order> Order(World world, CPos xy, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, mi);
		}

		IEnumerable<Order> OrderInner(World world, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var underCursor = world.ScreenMap.ActorsAt(mi)
					.FirstOrDefault(a => !world.FogObscures(a) && a.AppearsFriendlyTo(world.LocalPlayer.PlayerActor) && a.HasTrait<RepairableBuilding>());

				if (underCursor == null)
					yield break;

				if (underCursor.Info.Traits.Contains<RepairableBuildingInfo>()
					&& underCursor.GetDamageState() > DamageState.Undamaged)
					yield return new Order(OrderCode.RepairBuilding, world.LocalPlayer.PlayerActor, false) { TargetActor = underCursor };
			}
		}

		public void Tick(World world)
		{
			if (world.LocalPlayer != null &&
				world.LocalPlayer.WinState != WinState.Undefined)
				world.CancelInputMode();
		}

		public IEnumerable<IRenderable> Render(WorldRenderer wr, World world) { yield break; }
		public IEnumerable<IRenderable> RenderAfterWorld(WorldRenderer wr, World world) { yield break; }

		public string GetCursor(World world, CPos xy, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, mi).Any()
				? "repair" : "repair-blocked";
		}
	}
}
