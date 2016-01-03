#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ButtonTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ButtonTooltipLogic(Widget widget, ButtonWidget button)
		{
			var label = widget.Get<LabelWidget>("LABEL");
			var text = button.GetTooltipText();

			label.GetText = () => text;
			var horizontalPadding = widget.Bounds.Width - label.Bounds.Width;
			if (horizontalPadding <= 0 || label.Bounds.Width <= 0)
				horizontalPadding = 2 * label.Bounds.X;
			var verticalPadding = widget.Bounds.Height - label.Bounds.Height;
			if (verticalPadding <= 0 || label.Bounds.Height <= 0)
				verticalPadding = 2 * label.Bounds.Y + label.LinePixelSpacing; // With hang space

			var textSize = label.ResizeToText(text);
			widget.Bounds.Width = textSize.X + horizontalPadding;
			widget.Bounds.Height = textSize.Y + verticalPadding;

			if (button.Key.IsValid())
			{
				var hotkey = widget.Get<LabelWidget>("HOTKEY");
				hotkey.Visible = true;

				var hotkeyLabel = "({0})".F(button.Key.DisplayString());
				hotkey.GetText = () => hotkeyLabel;
				hotkey.Bounds.X = widget.Bounds.Width;
				hotkey.ResizeToText(hotkeyLabel);

				widget.Bounds.Width = hotkey.Bounds.X + label.Bounds.X + hotkey.Bounds.Width;
			}
		}
	}
}