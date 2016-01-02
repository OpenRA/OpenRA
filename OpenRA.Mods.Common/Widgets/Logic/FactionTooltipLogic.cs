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
			header.GetText = () => headerLine;

			var verticalPadding = widget.Bounds.Height - header.Bounds.Height;
			if (verticalPadding <= 0)
				verticalPadding = 2 * header.Bounds.Y + header.LinePixelSpacing; // With hang space

			header.ResizeToText(headerLine);

			if (lines.Length > 1)
			{
				var description = widget.Get<LabelWidget>("DESCRIPTION");
				var maxRight = Math.Max(header.Bounds.Right, description.Bounds.Right);
				var minLeft = Math.Min(header.Bounds.Left, description.Bounds.Left);
				var horizontalPadding = widget.Bounds.Width  + minLeft - maxRight;
				if (horizontalPadding <= 0)
					horizontalPadding = 2 * Math.Min(header.Bounds.X, description.Bounds.X);
				var separatorPadding = description.Bounds.Y - header.Bounds.Bottom;
				if (separatorPadding <= 0)
					separatorPadding = header.LinePixelSpacing;

				var descriptionLines = lines.Skip(1).ToArray();
				description.Bounds.Y = header.Bounds.Bottom + separatorPadding;
				description.Text = string.Join("\n", descriptionLines);
				description.ResizeToText(description.Text);

				maxRight = Math.Max(header.Bounds.Right, description.Bounds.Right);
				minLeft = Math.Min(header.Bounds.Left, description.Bounds.Left);
				widget.Bounds.Width = maxRight - minLeft + horizontalPadding;
				widget.Bounds.Height = description.Bounds.Bottom + verticalPadding - header.Bounds.Y;
			}
			else
			{
				var horizontalPadding = widget.Bounds.Width - header.Bounds.Width;
				if (horizontalPadding <= 0)
					horizontalPadding = 2 * header.Bounds.X;

				widget.Bounds.Width = header.Bounds.Right + horizontalPadding;
				widget.Bounds.Height = header.Bounds.Bottom + verticalPadding - header.Bounds.Y;
			}
		}
	}
}