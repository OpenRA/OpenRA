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

using OpenRA.Mods.Common.Orders;
using OpenRA.Orders;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public abstract class ChromeOrderButtonLogic<T> : ChromeLogic where T : IOrderGenerator
	{
		protected ChromeOrderButtonLogic(Widget w, World world, string icon)
		{
			if (w is not ButtonWidget widget)
				return;

			widget.OnClick = () =>
			{
				if (world.OrderGenerator is T)
					world.CancelInputMode();
				else
					world.OrderGenerator = (IOrderGenerator)typeof(T).GetConstructor([typeof(World)])?.Invoke([world]);
			};

			widget.IsHighlighted = () => world.OrderGenerator is T;

			widget.Get<ImageWidget>("ICON").GetImageName =
				() => world.OrderGenerator is T ? icon + "-active" : icon;
		}
	}

	[method: ObjectCreator.UseCtor]
	public class SellOrderButtonLogic(Widget widget, World world)
		: ChromeOrderButtonLogic<SellOrderGenerator>(widget, world, "sell");

	[method: ObjectCreator.UseCtor]
	public class RepairOrderButtonLogic(Widget widget, World world)
		: ChromeOrderButtonLogic<RepairOrderGenerator>(widget, world, "repair");

	[method: ObjectCreator.UseCtor]
	public class PowerdownOrderButtonLogic(Widget widget, World world)
		: ChromeOrderButtonLogic<PowerDownOrderGenerator>(widget, world, "power");

	[method: ObjectCreator.UseCtor]
	public class BeaconOrderButtonLogic(Widget widget, World world)
		: ChromeOrderButtonLogic<BeaconOrderGenerator>(widget, world, "beacon");
}
