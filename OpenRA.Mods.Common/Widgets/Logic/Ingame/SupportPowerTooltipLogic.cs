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
using System.Drawing;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SupportPowerTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SupportPowerTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, SupportPowersWidget palette, World world, PlayerResources playerResources)
		{
			widget.IsVisible = () => palette.TooltipIcon != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var descLabel = widget.Get<LabelWidget>("DESC");
			var costLabel = widget.Get<LabelWidget>("COST");
			var nameFont = Game.Renderer.Fonts[nameLabel.Font];
			var timeFont = Game.Renderer.Fonts[timeLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var costFont = Game.Renderer.Fonts[costLabel.Font];
			var name = "";
			var time = "";
			var desc = "";
			var baseHeight = widget.Bounds.Height;
			var timeOffset = timeLabel.Bounds.X;
			var costOffset = costLabel.Bounds.X;

			SupportPowerInstance lastPower = null;
			tooltipContainer.BeforeRender = () =>
			{
				var icon = palette.TooltipIcon;

				if (icon == null)
					return;

				var sp = icon.Power;

				if (sp.Info == null)
					return;		// no instances actually exist (race with destroy)

				var remaining = WidgetUtils.FormatTime(sp.RemainingTime, world.Timestep);
				var total = WidgetUtils.FormatTime(sp.Info.ChargeTime * 25, world.Timestep);
				time = "{0} / {1}".F(remaining, total);

				if (sp == lastPower)
					return;

				var cost = sp.Info.Cost;
				var costString = costLabel.Text + cost.ToString();
				costLabel.GetText = () => costString;
				costLabel.GetColor = () => playerResources.Cash + playerResources.Resources >= cost
					? Color.White : Color.Red;
				costLabel.IsVisible = () => cost != 0;

				name = sp.Info.Description;
				desc = sp.Info.LongDesc.Replace("\\n", "\n");

				var hotkey = icon.Hotkey;
				var hotkeyText = "({0})".F(hotkey.DisplayString());
				var hotkeyWidth = hotkey.IsValid() ? nameFont.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X : 0;
				hotkeyLabel.GetText = () => hotkeyText;
				hotkeyLabel.Bounds.X = nameFont.Measure(name).X + 2 * nameLabel.Bounds.X;
				hotkeyLabel.Visible = hotkey.IsValid();

				var timeWidth = timeFont.Measure(time).X;
				var costWidth = costFont.Measure(costString).X;
				var topWidth = nameFont.Measure(name).X + hotkeyWidth + timeWidth + timeOffset;

				if (cost != 0)
				{
					topWidth += costWidth + costOffset;
				}

				var descSize = descFont.Measure(desc);
				widget.Bounds.Width = 2 * nameLabel.Bounds.X + Math.Max(topWidth, descSize.X);
				widget.Bounds.Height = baseHeight + descSize.Y;
				timeLabel.Bounds.X = widget.Bounds.Width - nameLabel.Bounds.X - timeWidth;

				if (cost != 0)
				{
					timeLabel.Bounds.X -= costWidth + costOffset;
					costLabel.Bounds.X = widget.Bounds.Width - nameLabel.Bounds.X - costWidth;
				}

				lastPower = sp;
			};

			nameLabel.GetText = () => name;
			timeLabel.GetText = () => time;
			descLabel.GetText = () => desc;
		}
	}
}
