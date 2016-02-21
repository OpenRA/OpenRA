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

using System;
using System.Linq;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class FactionTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public FactionTooltipLogic(Widget widget, ButtonWidget button)
		{
			var lines = button.GetTooltipText().Replace("\\n", "\n").Split('\n');

			var header = widget.Get<LabelWidget>("HEADER");
			var headerLine = lines[0];
			var headerFont = Game.Renderer.Fonts[header.Font];
			var headerSize = headerFont.Measure(headerLine);
			header.Bounds.Width += headerSize.X;
			header.Bounds.Height += headerSize.Y;
			header.GetText = () => headerLine;

			if (lines.Length > 1)
			{
				var description = widget.Get<LabelWidget>("DESCRIPTION");
				var descriptionLines = lines.Skip(1).ToArray();
				var descriptionFont = Game.Renderer.Fonts[description.Font];
				description.Bounds.Y += header.Bounds.Y + header.Bounds.Height;
				description.Bounds.Width += descriptionLines.Select(l => descriptionFont.Measure(l).X).Max();
				description.Bounds.Height += descriptionFont.Measure(descriptionLines.First()).Y * descriptionLines.Length;
				description.GetText = () => string.Join("\n", descriptionLines);

				widget.Bounds.Width = Math.Max(header.Bounds.X + header.Bounds.Width, description.Bounds.X + description.Bounds.Width);
				widget.Bounds.Height = description.Bounds.Y + description.Bounds.Height;
			}
			else
			{
				widget.Bounds.Width = header.Bounds.X + header.Bounds.Width;
				widget.Bounds.Height = header.Bounds.Y + header.Bounds.Height;
			}
		}
	}
}