#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
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
			label.Node.Width = labelWidth;
			label.Node.CalculateLayout();
			widget.Node.Width = 2 * (int)label.Node.LayoutX + labelWidth;
			widget.Node.CalculateLayout();

			if (key.IsValid())
			{
				var hotkey = widget.Get<LabelWidget>("HOTKEY");
				hotkey.Visible = true;

				var hotkeyLabel = "({0})".F(key.DisplayString());
				hotkey.GetText = () => hotkeyLabel;
				hotkey.Node.Left = labelWidth + 2 * (int)label.Node.LayoutX;
				hotkey.Node.CalculateLayout();

				widget.Node.Width = (int)hotkey.Node.LayoutX + (int)label.Node.LayoutX + font.Measure(hotkeyLabel).X;
				widget.Node.CalculateLayout();
			}

			var desc = button.GetTooltipDesc();
			if (!string.IsNullOrEmpty(desc))
			{
				var descTemplate = widget.Get<LabelWidget>("DESC");
				widget.RemoveChild(descTemplate);

				var descFont = Game.Renderer.Fonts[descTemplate.Font];
				var descWidth = 0;
				var descOffset = (int)descTemplate.Node.LayoutY;
				foreach (var line in desc.Split(new[] { "\\n" }, StringSplitOptions.None))
				{
					descWidth = Math.Max(descWidth, descFont.Measure(line).X);
					var lineLabel = (LabelWidget)descTemplate.Clone();
					lineLabel.GetText = () => line;
					lineLabel.Node.Top = descOffset;
					lineLabel.Node.CalculateLayout();
					widget.AddChild(lineLabel);
					descOffset += (int)descTemplate.Node.LayoutHeight;
				}

				widget.Node.Width = Math.Max((int)widget.Node.LayoutWidth, (int)descTemplate.Node.LayoutX * 2 + descWidth);
				widget.Node.Height = (int)widget.Node.LayoutHeight + descOffset - (int)descTemplate.Node.LayoutY + (int)descTemplate.Node.LayoutX;
				widget.Node.CalculateLayout();
			}
		}
	}
}
