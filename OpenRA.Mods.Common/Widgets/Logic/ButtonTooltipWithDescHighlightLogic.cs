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
using System.Collections.Generic;
using OpenRA.Primitives;
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
				var highlightColor = FieldLoader.GetValue<Color>("Highlight", logicArgs["Highlight"].Value);
				widget.RemoveChild(descTemplate);

				var descFont = Game.Renderer.Fonts[descTemplate.Font];
				var descWidth = 0;
				var descOffset = (int)descTemplate.LayoutY;

				foreach (var l in desc.Split(new[] { "\\n" }, StringSplitOptions.None))
				{
					var line = l;
					var lineWidth = 0;

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
								lineNormalLabel.Left = (int)descTemplate.LayoutX + lineWidth;
								lineNormalLabel.Top = descOffset;
								lineNormalLabel.Width = lineNormalWidth;
								widget.AddChild(lineNormalLabel);

								lineWidth += lineNormalWidth;
							}

							// Highlight line segment
							var lineHighlight = line.Substring(highlightStart + 1, highlightEnd - highlightStart - 1);
							var lineHighlightWidth = descFont.Measure(lineHighlight).X;
							var lineHighlightLabel = (LabelWidget)descTemplate.Clone();
							lineHighlightLabel.GetText = () => lineHighlight;
							lineHighlightLabel.GetColor = () => highlightColor;
							lineHighlightLabel.Left = (int)descTemplate.LayoutX + lineWidth;
							lineHighlightLabel.Top = descOffset;
							lineHighlightLabel.Width = lineHighlightWidth;
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
							lineLabel.Left = (int)descTemplate.LayoutX + lineWidth;
							lineLabel.Top = descOffset;
							widget.AddChild(lineLabel);

							lineWidth += width;
							break;
						}
					}

					descWidth = Math.Max(descWidth, lineWidth);

					descOffset += (int)descTemplate.LayoutHeight;
				}

				widget.Width = Math.Max((int)widget.LayoutWidth, (int)descTemplate.LayoutX * 2 + descWidth);
				widget.Height = (int)widget.LayoutHeight + descOffset - (int)descTemplate.LayoutY + (int)descTemplate.LayoutX;
			}
		}
	}
}
