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

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SupportPowerBinLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SupportPowerBinLogic(Widget widget)
		{
			var palette = widget.Get<SupportPowersWidget>("SUPPORT_PALETTE");

			var background = widget.GetOrNull("PALETTE_BACKGROUND");
			var foreground = widget.GetOrNull("PALETTE_FOREGROUND");
			if (background != null || foreground != null)
			{
				Widget backgroundTemplate = null;
				Widget foregroundTemplate = null;

				if (background != null)
					backgroundTemplate = background.Get("ICON_TEMPLATE");

				if (foreground != null)
					foregroundTemplate = foreground.Get("ICON_TEMPLATE");

				Action<int, int> updateBackground = (_, icons) =>
				{
					var rowHeight = palette.IconSize.Y + palette.IconMargin;
					var rowWidth = palette.IconSize.X + palette.IconMargin;

					if (background != null)
					{
						background.RemoveChildren();

						for (var i = 0; i < icons; i++)
						{
							var row = backgroundTemplate.Clone();
							if (palette.Horizontal)
								row.Bounds.X += i * rowWidth;
							else
								row.Bounds.Y += i * rowHeight;
							background.AddChild(row);
						}
					}

					if (foreground != null)
					{
						foreground.RemoveChildren();

						for (var i = 0; i < icons; i++)
						{
							var row = foregroundTemplate.Clone();
							if (palette.Horizontal)
								row.Bounds.X += i * rowWidth;
							else
								row.Bounds.Y += i * rowHeight;
							foreground.AddChild(row);
						}
					}
				};

				palette.OnIconCountChanged += updateBackground;

				// Set the initial palette state
				updateBackground(0, 0);
			}
		}
	}
}
