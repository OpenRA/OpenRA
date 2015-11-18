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
			var font = Game.Renderer.Fonts[label.Font];
			var text = button.GetTooltipText();
			var textDims = font.Measure(text, label.LineSpacing);

			label.GetText = () => text;
			label.Bounds.Width = textDims.X;
			label.Bounds.Height = textDims.Y;
			var horizontalPadding = widget.Bounds.Width - label.Bounds.Width;
			if (horizontalPadding <= 0 || label.Bounds.Width <= 0)
				horizontalPadding = 2 * label.Bounds.X;
			var verticalPadding = widget.Bounds.Height - label.Bounds.Height;
			if (verticalPadding <= 0 || label.Bounds.Height <= 0)
				verticalPadding = 2 * label.Bounds.Y + 2 * font.Size / 5; // With hang space
			widget.Bounds.Width = textDims.X + horizontalPadding;
			widget.Bounds.Height = textDims.Y + verticalPadding;

			if (button.Key.IsValid())
			{
				var hotkey = widget.Get<LabelWidget>("HOTKEY");
				hotkey.Visible = true;

				var hotkeyLabel = "({0})".F(button.Key.DisplayString());
				hotkey.GetText = () => hotkeyLabel;
				hotkey.Bounds.X = widget.Bounds.Width;
				var hotkeyTextDims = font.Measure(hotkeyLabel, hotkey.LineSpacing);
				hotkey.Bounds.Height = hotkeyTextDims.Y;
				hotkey.Bounds.Width = hotkeyTextDims.X;

				widget.Bounds.Width = hotkey.Bounds.X + label.Bounds.X + hotkey.Bounds.Width;
			}
		}
	}
}