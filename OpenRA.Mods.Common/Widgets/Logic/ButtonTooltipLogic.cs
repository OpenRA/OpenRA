#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
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
			var labelWidth = font.Measure(text).X;

			label.GetText = () => text;
			label.Bounds.Width = labelWidth;
			widget.Bounds.Width = 2 * label.Bounds.X + labelWidth;

			if (button.Key.IsValid())
			{
				var hotkey = widget.Get<LabelWidget>("HOTKEY");
				hotkey.Visible = true;

				var hotkeyLabel = "({0})".F(button.Key.DisplayString());
				hotkey.GetText = () => hotkeyLabel;
				hotkey.Bounds.X = labelWidth + 2 * label.Bounds.X;

				widget.Bounds.Width = hotkey.Bounds.X + label.Bounds.X + font.Measure(hotkeyLabel).X;
			}
		}
	}
}