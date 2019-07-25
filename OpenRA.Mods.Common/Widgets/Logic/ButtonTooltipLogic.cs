#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
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
	public class ButtonTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ButtonTooltipLogic(Widget widget, ButtonWidget button)
		{
			var label = widget.Get<LabelWidget>("LABEL");
			var font = Game.Renderer.Fonts[label.Font];
			var text = button.GetTooltipText();
			var labelWidth = font.Measure(text).X;
			var key = button.Key.GetValue();

			label.GetText = () => text;
			label.Width = labelWidth;
			widget.Width = 2 * (int)label.LayoutX + labelWidth;

			if (key.IsValid())
			{
				var hotkey = widget.Get<LabelWidget>("HOTKEY");
				hotkey.Visible = true;

				var hotkeyLabel = "({0})".F(key.DisplayString());
				hotkey.GetText = () => hotkeyLabel;
				hotkey.Left = labelWidth + 2 * (int)label.LayoutX;

				widget.Width = (int)hotkey.LayoutX + (int)label.LayoutX + font.Measure(hotkeyLabel).X;
			}

			var desc = button.GetTooltipDesc();
			if (!string.IsNullOrEmpty(desc))
			{
				var descTemplate = widget.Get<LabelWidget>("DESC");
				widget.RemoveChild(descTemplate);

				var descFont = Game.Renderer.Fonts[descTemplate.Font];
				var descWidth = 0;
				var descOffset = (int)descTemplate.LayoutY;
				foreach (var line in desc.Split(new[] { "\\n" }, StringSplitOptions.None))
				{
					descWidth = Math.Max(descWidth, descFont.Measure(line).X);
					var lineLabel = (LabelWidget)descTemplate.Clone();
					lineLabel.GetText = () => line;
					lineLabel.Top = descOffset;
					widget.AddChild(lineLabel);
					descOffset += (int)descTemplate.LayoutHeight;
				}

				widget.Width = Math.Max((int)widget.LayoutWidth, (int)descTemplate.LayoutX * 2 + descWidth);
				widget.Height = (int)widget.LayoutHeight + descOffset - (int)descTemplate.LayoutY + (int)descTemplate.LayoutX;
			}
		}
	}
}
