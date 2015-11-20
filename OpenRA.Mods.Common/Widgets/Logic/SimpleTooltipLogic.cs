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

			var cachedWidth = 0;
			var cachedHeight = 0;
			var horizontalPadding = widget.Bounds.Width - label.Bounds.Width;
			if (horizontalPadding <= 0 || label.Bounds.Width <= 0)
				horizontalPadding = 2 * label.Bounds.X;
			var verticalPadding = widget.Bounds.Height - label.Bounds.Height;
			if (verticalPadding <= 0 || label.Bounds.Height <= 0)
				verticalPadding = 2 * label.Bounds.Y + label.LinePixelSpacing; // With hang space
			var labelText = "";
			tooltipContainer.BeforeRender = () =>
			{
				labelText = getText();
				var textSize = label.ResizeToText(labelText);

				if (textSize.X != cachedWidth || textSize.Y != cachedHeight)
				{
					widget.Bounds.Width = horizontalPadding + textSize.X;
					widget.Bounds.Height = verticalPadding + textSize.Y;
					cachedWidth = textSize.X;
					cachedHeight = textSize.Y;
				}
			};

			label.GetText = () => labelText;
		}
	}
}