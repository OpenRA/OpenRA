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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ArmyTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ArmyTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, Func<ArmyUnit> getTooltipUnit)
		{
			widget.VisibilityFunction = () => getTooltipUnit() != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var descLabel = widget.Get<LabelWidget>("DESC");

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];

			ArmyUnit lastArmyUnit = null;
			var descLabelPadding = (int)descLabel.Node.LayoutHeight;

			tooltipContainer.BeforeRender = () =>
			{
				var armyUnit = getTooltipUnit();

				if (armyUnit == null || armyUnit == lastArmyUnit)
					return;

				var tooltip = armyUnit.TooltipInfo;
				var name = tooltip != null ? tooltip.Name : armyUnit.ActorInfo.Name;
				var buildable = armyUnit.BuildableInfo;

				nameLabel.Text = name;

				var nameSize = font.Measure(name);

				descLabel.Text = buildable.Description.Replace("\\n", "\n");
				var descSize = descFont.Measure(descLabel.Text);
				descLabel.Node.Width = descSize.X;
				descLabel.Node.Height = descSize.Y + descLabelPadding;
				descLabel.Node.CalculateLayout();

				var leftWidth = Math.Max(nameSize.X, descSize.X);

				widget.Node.Width = leftWidth + 2 * (int)nameLabel.Node.LayoutX;
				widget.Node.CalculateLayout();

				// Set the bottom margin to match the left margin
				var leftHeight = (int)(descLabel.Node.LayoutY + descLabel.Node.LayoutHeight) + (int)descLabel.Node.LayoutX;

				widget.Node.Height = leftHeight;
				widget.Node.CalculateLayout();

				lastArmyUnit = armyUnit;
			};
		}
	}
}
