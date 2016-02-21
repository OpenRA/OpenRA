#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
			var cachedHeight = 0;
			var horizontalPadding = label.Bounds.Width - widget.Bounds.Width;
			if (horizontalPadding <= 0)
				horizontalPadding = 2 * label.Bounds.X;
			var vertcalPadding = widget.Bounds.Height - label.Bounds.Height;
			if (vertcalPadding <= 0)
				vertcalPadding = 2 * label.Bounds.Y;
			var labelText = "";
			tooltipContainer.BeforeRender = () =>
			{
				labelText = getText();
				var textDim = font.Measure(labelText);
				if (textDim.X != cachedWidth || textDim.Y != cachedHeight)
				{
					label.Bounds.Width = textDim.X;
					widget.Bounds.Width = horizontalPadding + textDim.X;
					label.Bounds.Height = textDim.Y;
					widget.Bounds.Height = vertcalPadding + textDim.Y;
					cachedWidth = textDim.X;
					cachedHeight = textDim.Y;
				}
			};

			label.GetText = () => labelText;
		}
	}
}