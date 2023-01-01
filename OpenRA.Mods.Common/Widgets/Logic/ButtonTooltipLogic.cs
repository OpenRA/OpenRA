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
			label.Bounds.Width = labelWidth;
			widget.Bounds.Width = 2 * label.Bounds.X + labelWidth;

			if (key.IsValid())
			{
				var hotkey = widget.Get<LabelWidget>("HOTKEY");
				hotkey.Visible = true;

				var hotkeyLabel = $"({key.DisplayString()})";
				hotkey.GetText = () => hotkeyLabel;
				hotkey.Bounds.X = labelWidth + 2 * label.Bounds.X;

				widget.Bounds.Width = hotkey.Bounds.X + label.Bounds.X + font.Measure(hotkeyLabel).X;
			}

			var desc = button.GetTooltipDesc();
			if (!string.IsNullOrEmpty(desc))
			{
				var descTemplate = widget.Get<LabelWidget>("DESC");
				widget.RemoveChild(descTemplate);

				var descFont = Game.Renderer.Fonts[descTemplate.Font];
				var descWidth = 0;
				var descOffset = descTemplate.Bounds.Y;
				foreach (var line in desc.Split(new[] { "\\n" }, StringSplitOptions.None))
				{
					descWidth = Math.Max(descWidth, descFont.Measure(line).X);
					var lineLabel = (LabelWidget)descTemplate.Clone();
					lineLabel.GetText = () => line;
					lineLabel.Bounds.Y = descOffset;
					widget.AddChild(lineLabel);
					descOffset += descTemplate.Bounds.Height;
				}

				widget.Bounds.Width = Math.Max(widget.Bounds.Width, descTemplate.Bounds.X * 2 + descWidth);
				widget.Bounds.Height += descOffset - descTemplate.Bounds.Y + descTemplate.Bounds.X;
			}
		}
	}
}
