#region Copyright & License Information
/*
 * Copyright 2007-2017 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ButtonTooltipWithDescHighlightLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ButtonTooltipWithDescHighlightLogic(Widget widget, ButtonWidget button, Dictionary<string, MiniYaml> logicArgs)
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

			var desc = button.GetTooltipDesc();
			if (!string.IsNullOrEmpty(desc))
			{
				var descTemplate = widget.Get<LabelWidget>("DESC");
				var highlightColor = FieldLoader.GetValue<Color>("Highlight", logicArgs["Highlight"].Value);
				widget.RemoveChild(descTemplate);

				var descFont = Game.Renderer.Fonts[descTemplate.Font];
				var descWidth = 0;
				var descOffset = descTemplate.Bounds.Y;

				foreach (var l in desc.Split(new[] { "\\n" }, StringSplitOptions.None))
				{
					var line = l;
					var lineWidth = 0;
					var xOffset = descTemplate.Bounds.X;

					while (line.Length > 0)
					{
						var highlightStart = line.IndexOf('{');
						var highlightEnd = line.IndexOf('}', 0);

						if (highlightStart > 0 && highlightEnd > highlightStart)
						{
							if (highlightStart > 0)
							{
								// Normal line segment before highlight
								var lineNormal = line.Substring(0, highlightStart);
								var lineNormalWidth = descFont.Measure(lineNormal).X;
								var lineNormalLabel = (LabelWidget)descTemplate.Clone();
								lineNormalLabel.GetText = () => lineNormal;
								lineNormalLabel.Bounds.X = descTemplate.Bounds.X + lineWidth;
								lineNormalLabel.Bounds.Y = descOffset;
								lineNormalLabel.Bounds.Width = lineNormalWidth;
								widget.AddChild(lineNormalLabel);

								lineWidth += lineNormalWidth;
							}

							// Highlight line segment
							var lineHighlight = line.Substring(highlightStart + 1, highlightEnd - highlightStart - 1);
							var lineHighlightWidth = descFont.Measure(lineHighlight).X;
							var lineHighlightLabel = (LabelWidget)descTemplate.Clone();
							lineHighlightLabel.GetText = () => lineHighlight;
							lineHighlightLabel.GetColor = () => highlightColor;
							lineHighlightLabel.Bounds.X = descTemplate.Bounds.X + lineWidth;
							lineHighlightLabel.Bounds.Y = descOffset;
							lineHighlightLabel.Bounds.Width = lineHighlightWidth;
							widget.AddChild(lineHighlightLabel);

							lineWidth += lineHighlightWidth;
							line = line.Substring(highlightEnd + 1);
						}
						else
						{
							// Final normal line segment
							var lineLabel = (LabelWidget)descTemplate.Clone();
							var width = descFont.Measure(line).X;
							lineLabel.GetText = () => line;
							lineLabel.Bounds.X = descTemplate.Bounds.X + lineWidth;
							lineLabel.Bounds.Y = descOffset;
							widget.AddChild(lineLabel);

							lineWidth += width;
							break;
						}
					}

					descWidth = Math.Max(descWidth, lineWidth);

					descOffset += descTemplate.Bounds.Height;
				}

				widget.Bounds.Width = Math.Max(widget.Bounds.Width, descTemplate.Bounds.X * 2 + descWidth);
				widget.Bounds.Height += descOffset - descTemplate.Bounds.Y + descTemplate.Bounds.X;
			}
		}
	}
}