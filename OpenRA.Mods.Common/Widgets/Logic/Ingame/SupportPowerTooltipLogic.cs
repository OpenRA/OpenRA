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
using OpenRA.Mods.Common.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class SupportPowerTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public SupportPowerTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, SupportPowersWidget palette, World world)
		{
			widget.IsVisible = () => palette.TooltipIcon != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var descLabel = widget.Get<LabelWidget>("DESC");
			var name = "";
			var time = "";
			var desc = "";
			var baseHeight = widget.Bounds.Height;
			var timeOffset = timeLabel.Bounds.X;

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

				name = sp.Info.Description;
				desc = sp.Info.LongDesc.Replace("\\n", "\n");

				var hotkey = icon.Hotkey;
				var hotkeyText = "({0})".F(hotkey.DisplayString());
				var hotkeyWidth = hotkey.IsValid() ? hotkeyLabel.MeasureText(hotkeyText).X + 2 * nameLabel.Bounds.X : 0;
				hotkeyLabel.GetText = () => hotkeyText;
				hotkeyLabel.Bounds.X = nameLabel.MeasureText(name).X + 2 * nameLabel.Bounds.X;
				hotkeyLabel.Visible = hotkey.IsValid();

				var timeWidth = timeLabel.MeasureText(time).X;
				var topWidth = nameLabel.MeasureText(name).X + hotkeyWidth + timeWidth + timeOffset;
				var descSize = descLabel.MeasureText(desc);
				widget.Bounds.Width = 2 * nameLabel.Bounds.X + Math.Max(topWidth, descSize.X);
				widget.Bounds.Height = baseHeight + descSize.Y;
				timeLabel.Bounds.X = widget.Bounds.Width - nameLabel.Bounds.X - timeWidth;
				lastPower = sp;
			};

			nameLabel.GetText = () => name;
			timeLabel.GetText = () => time;
			descLabel.GetText = () => desc;
		}
	}
}