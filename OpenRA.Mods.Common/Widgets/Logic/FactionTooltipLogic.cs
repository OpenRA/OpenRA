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
			var headerSize = header.MeasureText(headerLine);
			header.Bounds.Width += headerSize.X;
			header.Bounds.Height += headerSize.Y;
			header.GetText = () => headerLine;

			if (lines.Length > 1)
			{
				var description = widget.Get<LabelWidget>("DESCRIPTION");
				var descriptionLines = lines.Skip(1).ToArray();
				description.Bounds.Y += header.Bounds.Y + header.Bounds.Height;
				description.Text = string.Join("\n", descriptionLines);
				var descriptionSize = description.MeasureText(description.Text);
				description.Bounds.Width += descriptionSize.X;
				description.Bounds.Height += descriptionSize.Y;

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