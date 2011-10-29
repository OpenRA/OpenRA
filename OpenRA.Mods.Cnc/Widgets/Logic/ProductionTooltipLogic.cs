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
using System.Drawing;
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class ProductionTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public ProductionTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, ProductionPaletteWidget palette)
		{
			var pm = palette.world.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var pr = palette.world.LocalPlayer.PlayerActor.Trait<PlayerResources>();

			widget.IsVisible = () => palette.TooltipActor != null;
			var nameLabel = widget.GetWidget<LabelWidget>("NAME");
			var requiresLabel = widget.GetWidget<LabelWidget>("REQUIRES");
			var powerLabel = widget.GetWidget<LabelWidget>("POWER");
			var timeLabel = widget.GetWidget<LabelWidget>("TIME");
			var costLabel = widget.GetWidget<LabelWidget>("COST");

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var requiresFont = Game.Renderer.Fonts[requiresLabel.Font];
			string lastActor = null;

			tooltipContainer.BeforeRender = () =>
			{
				var actor = palette.TooltipActor;
				if (actor == null || actor == lastActor)
					return;

				var info = Rules.Info[actor];
				var tooltip = info.Traits.Get<TooltipInfo>();
				var buildable = info.Traits.Get<BuildableInfo>();
				var cost = info.Traits.Get<ValuedInfo>().Cost;
				var bi = info.Traits.GetOrDefault<BuildingInfo>();

				nameLabel.GetText = () => tooltip.Name;

				var prereqs = buildable.Prerequisites.Select(a => ActorName(a));
				var requiresString = prereqs.Any() ? "Requires {0}".F(prereqs.JoinWith(", ")) : "";
				requiresLabel.GetText = () => requiresString;

				var power = bi != null ? bi.Power : 0;
				var powerString = "P: {0}".F(power);
				powerLabel.GetText = () => powerString;
				powerLabel.GetColor = () => ((pm.PowerProvided - pm.PowerDrained) >= -power || power > 0)
					? Color.White : Color.Red;
				powerLabel.IsVisible = () => power != 0;

				var timeString = "T: {0}".F(WidgetUtils.FormatTime(palette.CurrentQueue.GetBuildTime(actor)));
				timeLabel.GetText = () => timeString;

				var costString = "$: {0}".F(cost);
				costLabel.GetText = () => costString;
				costLabel.GetColor = () => pr.DisplayCash + pr.DisplayOre >= cost
					? Color.White : Color.Red;

				var leftWidth = Math.Max(font.Measure(tooltip.Name).X, requiresFont.Measure(requiresString).X);
				var rightWidth = new [] {font.Measure(powerString).X, font.Measure(timeString).X, font.Measure(costString).X}.Aggregate(Math.Max);
				timeLabel.Bounds.X = powerLabel.Bounds.X = costLabel.Bounds.X = leftWidth + 2*nameLabel.Bounds.X;
				widget.Bounds.Width = leftWidth + rightWidth + 3*nameLabel.Bounds.X;

				widget.Bounds.Height = power != 0 ? 65 : 45;
				lastActor = actor;
			};
		}

		static string ActorName( string a )
		{
			// hack hack hack - going to die soon anyway
			if (a == "barracks")
				return "Infantry Production";
			if (a == "vehicleproduction")
				return "Vehicle Production";
			if (a == "techcenter")
				return "Tech Center";
			if (a == "anypower")
				return "Power Plant";

			ActorInfo ai;
			Rules.Info.TryGetValue(a.ToLowerInvariant(), out ai);
			if (ai != null && ai.Traits.Contains<TooltipInfo>())
				return ai.Traits.Get<TooltipInfo>().Name;

			return a;
		}
	}
}

