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

using System.Linq;
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ProductionQueueOverviewLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ProductionQueueOverviewLogic(Widget widget, World world)
		{
			if (world.LocalPlayer == null || world.LocalPlayer.Spectating)
				return;

			var overview = widget.Get<ProductionQueueOverviewWidget>("PRODUCTION_QUEUE_OVERVIEW");
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

			var ticker = widget.GetOrNull<LogicTickerWidget>("PRODUCTION_QUEUE_TICKER");
			if (ticker != null && sidebar is ContainerWidget sidebarContainer)
			{
				ticker.OnTick = () =>
				{
					var sidebarBottom = GetSidebarProductionBottom(sidebarContainer);
					widget.Bounds.X = sidebar.Bounds.X;
					widget.Bounds.Y = sidebarBottom + 4 - widget.Parent.RenderOrigin.Y;
				};
			}
		}

		static int GetSidebarProductionBottom(ContainerWidget sidebar)
		{
			var bottom = sidebar.RenderBounds.Bottom;

			var background = sidebar.GetOrNull("PALETTE_BACKGROUND");
			if (background != null)
			{
				foreach (var child in background.Children)
				{
					var childBottom = child.RenderBounds.Bottom;
					if (childBottom > bottom)
						bottom = childBottom;
				}
			}

			var types = sidebar.GetOrNull("PRODUCTION_TYPES");
			var scrollDown = types?.GetOrNull<ButtonWidget>("SCROLL_DOWN_BUTTON");
			if (scrollDown != null)
			{
				var scrollBottom = scrollDown.RenderBounds.Bottom;
				if (scrollBottom > bottom)
					bottom = scrollBottom;
			}

			return bottom;
		}
	}
}
