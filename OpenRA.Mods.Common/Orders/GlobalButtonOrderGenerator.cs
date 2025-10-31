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
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.Orders
{
	public abstract class GlobalButtonOrderGenerator<T> : OrderGenerator
	{
		readonly string order;

		protected GlobalButtonOrderGenerator(string order)
		{
			this.order = order;
		}

		protected override IEnumerable<Order> OrderInner(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			if (mi.Button == MouseButton.Right)
				world.CancelInputMode();

			return OrderInner(world, mi);
		}

		protected virtual bool IsValidTrait(T t)
		{
			return t.IsTraitEnabled();
		}

		protected IEnumerable<Order> OrderInner(World world, MouseInput mi)
		{
			if (mi.Button == MouseButton.Left)
			{
				var underCursor = world.ScreenMap.ActorsAtMouse(mi)
					.Select(a => a.Actor)
					.FirstOrDefault(a => a.Owner == world.LocalPlayer && a.TraitsImplementing<T>()
						.Any(IsValidTrait));

				if (underCursor == null)
					yield break;

				yield return new Order(order, underCursor, false);
			}
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

		protected abstract override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi);
	}

	public class PowerDownOrderGenerator : GlobalButtonOrderGenerator<ToggleConditionOnOrder>
	{
		public PowerDownOrderGenerator()
			: base("PowerDown") { }

		protected override bool IsValidTrait(ToggleConditionOnOrder t)
		{
			return !t.IsTraitDisabled && !t.IsTraitPaused;
		}

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			mi.Button = MouseButton.Left;
			return OrderInner(world, mi).Any() ? "powerdown" : "powerdown-blocked";
		}
	}

	public class SellOrderGenerator : GlobalButtonOrderGenerator<Sellable>
	{
		public SellOrderGenerator()
			: base("Sell") { }

		protected override string GetCursor(World world, CPos cell, int2 worldPixel, MouseInput mi)
		{
			mi.Button = MouseButton.Left;

			var cursor = OrderInner(world, mi)
				.SelectMany(o => o.Subject.TraitsImplementing<Sellable>())
				.Where(t => !t.IsTraitDisabled)
				.Select(si => si.Info.Cursor)
				.FirstOrDefault();

			return cursor ?? "sell-blocked";
		}
	}
}
