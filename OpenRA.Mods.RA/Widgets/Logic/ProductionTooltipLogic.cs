#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
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

namespace OpenRA.Mods.RA.Widgets.Logic
{
	public class ProductionTooltipLogic
	{
		[ObjectCreator.UseCtor]
		public ProductionTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, ProductionPaletteWidget palette)
		{
			var mapRules = palette.World.Map.Rules;
			var pm = palette.World.LocalPlayer.PlayerActor.Trait<PowerManager>();
			var pr = palette.World.LocalPlayer.PlayerActor.Trait<PlayerResources>();

			widget.IsVisible = () => palette.TooltipActor != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var requiresLabel = widget.Get<LabelWidget>("REQUIRES");
			var powerLabel = widget.Get<LabelWidget>("POWER");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var costLabel = widget.Get<LabelWidget>("COST");
			var descLabel = widget.Get<LabelWidget>("DESC");

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var requiresFont = Game.Renderer.Fonts[requiresLabel.Font];
			string lastActor = null;

			tooltipContainer.BeforeRender = () =>
			{
				var actor = palette.TooltipActor;
				if (actor == null || actor == lastActor)
					return;

				var info = mapRules.Actors[actor];
				var tooltip = info.Traits.Get<TooltipInfo>();
				var buildable = info.Traits.Get<BuildableInfo>();
				var cost = info.Traits.Get<ValuedInfo>().Cost;
				var bi = info.Traits.GetOrDefault<BuildingInfo>();

				nameLabel.GetText = () => tooltip.Name;

				var nameWidth = font.Measure(tooltip.Name).X;
				var hotkeyText = "({0})".F(buildable.Hotkey.DisplayString());
				var hotkeyWidth = buildable.Hotkey.IsValid() ? font.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X : 0;
				hotkeyLabel.GetText = () => hotkeyText;
				hotkeyLabel.Bounds.X = nameWidth + 2 * nameLabel.Bounds.X;
				hotkeyLabel.Visible = buildable.Hotkey.IsValid();


				var prereqs = buildable.Prerequisites.Select(a => ActorName(mapRules, a)).Where(s => !s.StartsWith("~"));
				var requiresString = prereqs.Any() ? requiresLabel.Text.F(prereqs.JoinWith(", ")) : "";
				requiresLabel.GetText = () => requiresString;

				var power = bi != null ? bi.Power : 0;
				var powerString = "P: {0}".F(power);
				powerLabel.GetText = () => powerString;
				powerLabel.GetColor = () => ((pm.PowerProvided - pm.PowerDrained) >= -power || power > 0)
					? Color.White : Color.Red;
				powerLabel.IsVisible = () => power != 0;

				var lowpower = pm.PowerState != PowerState.Normal;
				var time = palette.CurrentQueue == null ? 0 : palette.CurrentQueue.GetBuildTime(actor)
					* (lowpower ? palette.CurrentQueue.Info.LowPowerSlowdown : 1);
				var timeString = "T: {0}".F(WidgetUtils.FormatTime(time));
				timeLabel.GetText = () => timeString;
				timeLabel.GetColor = () => lowpower ? Color.Red : Color.White;

				var costString = "$: {0}".F(cost);
				costLabel.GetText = () => costString;
				costLabel.GetColor = () => pr.DisplayCash + pr.DisplayResources >= cost
					? Color.White : Color.Red;

				var descString = tooltip.Description.Replace("\\n", "\n");
				descLabel.GetText = () => descString;

				var leftWidth = new[] { nameWidth + hotkeyWidth, requiresFont.Measure(requiresString).X, descFont.Measure(descString).X }.Aggregate(Math.Max);
				var rightWidth = new[] { font.Measure(powerString).X, font.Measure(timeString).X, font.Measure(costString).X }.Aggregate(Math.Max);
				timeLabel.Bounds.X = powerLabel.Bounds.X = costLabel.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
				widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X;

				var leftHeight = font.Measure(tooltip.Name).Y + requiresFont.Measure(requiresString).Y + descFont.Measure(descString).Y;
				var rightHeight = font.Measure(powerString).Y + font.Measure(timeString).Y + font.Measure(costString).Y;
				widget.Bounds.Height = Math.Max(leftHeight, rightHeight) * 3 / 2 + 3 * nameLabel.Bounds.Y;

				lastActor = actor;
			};
		}

		static string ActorName(Ruleset rules, string a)
		{
			ActorInfo ai;
			if (rules.Actors.TryGetValue(a.ToLowerInvariant(), out ai) && ai.Traits.Contains<TooltipInfo>())
				return ai.Traits.Get<TooltipInfo>().Name;

			return a;
		}
	}
}