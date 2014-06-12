#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using OpenRA.Mods.Common;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class SupportPowerTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public SupportPowerTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, SupportPowersWidget palette)
		{
			widget.IsVisible = () => palette.TooltipPower != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var descLabel = widget.Get<LabelWidget>("DESC");
			var nameFont = Game.Renderer.Fonts[nameLabel.Font];
			var timeFont = Game.Renderer.Fonts[timeLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var name = "";
			var time = "";
			var desc = "";
			var baseHeight = widget.Bounds.Height;
			var timeOffset = timeLabel.Bounds.X;

			SupportPowerInstance lastPower = null;
			tooltipContainer.BeforeRender = () =>
			{
				var sp = palette.TooltipPower;
				if (sp == null)
					return;

				if (sp.Info == null)
					return;		// no instances actually exist (race with destroy)

				time = "{0} / {1}".F(WidgetUtils.FormatTime(sp.RemainingTime),
									 WidgetUtils.FormatTime(sp.Info.ChargeTime * 25));

				if (sp == lastPower)
					return;

				name = sp.Info.Description;
				desc = sp.Info.LongDesc.Replace("\\n", "\n");
				var timeWidth = timeFont.Measure(time).X;
				var topWidth = nameFont.Measure(name).X + timeWidth + timeOffset;
				var descSize = descFont.Measure(desc);
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