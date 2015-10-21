#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SimpleTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SimpleTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, Func<string> getText)
		{
			var label = widget.Get<LabelWidget>("LABEL");

			var font = Game.Renderer.Fonts[label.Font];
			var cachedWidth = 0;
			var labelText = "";
			tooltipContainer.BeforeRender = () =>
			{
				labelText = getText();
				var textDim = font.Measure(labelText);
				if (textDim.X != cachedWidth)
				{
					label.Bounds.Width = textDim.X;
					widget.Bounds.Width = 2 * label.Bounds.X + textDim.X;
					label.Bounds.Height = textDim.Y;
					widget.Bounds.Height = 4 * label.Bounds.Y + textDim.Y;
				}
			};

			label.GetText = () => labelText;
		}
	}
}