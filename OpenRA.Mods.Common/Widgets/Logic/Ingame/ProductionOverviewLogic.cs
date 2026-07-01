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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ProductionOverviewLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ProductionOverviewLogic(Widget widget, World world)
		{
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return;

			var overview = widget.Get<ProductionOverviewWidget>("PRODUCTION_OVERVIEW");
			var sidebar = Ui.Root.GetOrNull("SIDEBAR_PRODUCTION");
			var types = sidebar?.GetOrNull("PRODUCTION_TYPES");
			if (types == null)
				return;

			var buttons = types.Children
				.OfType<ProductionTypeButtonWidget>()
				.ToDictionary(b => b.ProductionGroup);

			overview.IsGroupDisabled = group => !buttons.TryGetValue(group, out var button) || button.IsDisabled();
			overview.TrySelectGroup = (group, modifiers) =>
			{
				if (!buttons.TryGetValue(group, out var button) || button.IsDisabled())
					return false;

				button.OnMouseUp(new MouseInput(MouseInputEvent.Up, MouseButton.Left, int2.Zero, int2.Zero, modifiers, 0));
				return true;
			};
		}
	}
}
