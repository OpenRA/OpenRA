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
using OpenRA.Mods.RA;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class SupportPowerTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public SupportPowerTooltipLogic([ObjectCreator.Param] Widget widget,
		                                [ObjectCreator.Param] TooltipContainerWidget tooltipContainer,
		                                [ObjectCreator.Param] SupportPowersWidget palette)
		{
			widget.IsVisible = () => palette.TooltipPower != null;
			var nameLabel = widget.GetWidget<LabelWidget>("NAME");
			var timeLabel = widget.GetWidget<LabelWidget>("TIME");
			var nameFont = Game.Renderer.Fonts[nameLabel.Font];
			var timeFont = Game.Renderer.Fonts[timeLabel.Font];
			var name = "";
			var time = "";

			SupportPowerManager.SupportPowerInstance lastPower = null;
			tooltipContainer.BeforeRender = () =>
			{
				var sp = palette.TooltipPower;
				if (sp == null)
					return;

				time = "{0} / {1}".F(WidgetUtils.FormatTime(sp.RemainingTime),
				                     WidgetUtils.FormatTime(sp.Info.ChargeTime*25));

				if (sp == lastPower)
					return;

				name = sp.Info.Description;
				widget.Bounds.Width = 2*nameLabel.Bounds.X
					+ Math.Max(nameFont.Measure(name).X, timeFont.Measure(time).X);
				lastPower = sp;
			};

			nameLabel.GetText = () => name;
			timeLabel.GetText = () => time;
		}
	}
}

