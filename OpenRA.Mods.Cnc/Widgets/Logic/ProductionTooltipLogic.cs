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
using System.Linq;
using OpenRA.Mods.RA;
using OpenRA.Mods.RA.Buildings;
using OpenRA.Support;
using OpenRA.Widgets;

namespace OpenRA.Mods.Cnc.Widgets.Logic
{
	public class ProductionTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public ProductionTooltipLogic([ObjectCreator.Param] Widget widget,
		                              [ObjectCreator.Param] TooltipContainerWidget tooltipContainer,
		                              [ObjectCreator.Param] ProductionPaletteWidget palette)
		{
			widget.IsVisible = () => palette.TooltipActor != null;
			var nameLabel = widget.GetWidget<LabelWidget>("NAME");
			var requiresLabel = widget.GetWidget<LabelWidget>("REQUIRES");
			var powerLabel = widget.GetWidget<LabelWidget>("POWER");
			var timeLabel = widget.GetWidget<LabelWidget>("TIME");
			var costLabel = widget.GetWidget<LabelWidget>("COST");

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var requiresFont = Game.Renderer.Fonts[requiresLabel.Font];
			var name = "";
			var requires = "";
			var power = "";
			var time = "";
			var cost = "";

			string lastActor = null;
			tooltipContainer.BeforeRender = () =>
			{
				var actor = palette.TooltipActor;
				if (actor == null || actor == lastActor)
					return;

				var info = Rules.Info[actor];
				var tooltip = info.Traits.Get<TooltipInfo>();
				var buildable = info.Traits.Get<BuildableInfo>();
				name = tooltip.Name;
				cost = "$: {0}".F(info.Traits.Get<ValuedInfo>().Cost);
				time = "T: {0}".F(WidgetUtils.FormatTime(palette.CurrentQueue.GetBuildTime(actor)));

				var bi = info.Traits.GetOrDefault<BuildingInfo>();
				power = bi != null ? "P: {0}".F(bi.Power) : "";

				var prereqs = buildable.Prerequisites.Select(a => ActorName(a));
				requires = prereqs.Any() ? "Requires {0}".F(string.Join(", ", prereqs.ToArray())) : "";

				var leftWidth = Math.Max(font.Measure(name).X, requiresFont.Measure(requires).X);
				var rightWidth = new [] {font.Measure(power).X, font.Measure(time).X, font.Measure(cost).X}.Aggregate(Math.Max);
				timeLabel.Bounds.X = powerLabel.Bounds.X = costLabel.Bounds.X = leftWidth + 2*nameLabel.Bounds.X;
				widget.Bounds.Width = leftWidth + rightWidth + 3*nameLabel.Bounds.X;
				lastActor = actor;

				widget.Bounds.Height = bi != null ? 65 : 45;
			};

			nameLabel.GetText = () => name;
			requiresLabel.GetText = () => requires;
			powerLabel.GetText = () => power;
			timeLabel.GetText = () => time;
			costLabel.GetText = () => cost;
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

