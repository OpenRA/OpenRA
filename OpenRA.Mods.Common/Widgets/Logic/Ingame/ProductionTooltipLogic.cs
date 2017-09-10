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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Support;
using OpenRA.Traits;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class ProductionTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public ProductionTooltipLogic(Widget widget, TooltipContainerWidget tooltipContainer, Player player, Func<ProductionIcon> getTooltipIcon)
		{
			var world = player.World;
			var mapRules = world.Map.Rules;
			var pm = player.PlayerActor.Trait<PowerManager>();
			var pr = player.PlayerActor.Trait<PlayerResources>();

			widget.IsVisible = () => getTooltipIcon() != null && getTooltipIcon().Actor != null;
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var hotkeyLabel = widget.Get<LabelWidget>("HOTKEY");
			var requiresLabel = widget.Get<LabelWidget>("REQUIRES");
			var powerLabel = widget.Get<LabelWidget>("POWER");
			var powerIcon = widget.Get<ImageWidget>("POWER_ICON");
			var timeLabel = widget.Get<LabelWidget>("TIME");
			var timeIcon = widget.Get<ImageWidget>("TIME_ICON");
			var costLabel = widget.Get<LabelWidget>("COST");
			var costIcon = widget.Get<ImageWidget>("COST_ICON");
			var descLabel = widget.Get<LabelWidget>("DESC");

			var iconMargin = timeIcon.Bounds.X;

			var font = Game.Renderer.Fonts[nameLabel.Font];
			var descFont = Game.Renderer.Fonts[descLabel.Font];
			var requiresFont = Game.Renderer.Fonts[requiresLabel.Font];
			ActorInfo lastActor = null;
			Hotkey lastHotkey = Hotkey.Invalid;

			tooltipContainer.BeforeRender = () =>
			{
				var tooltipIcon = getTooltipIcon();
				if (tooltipIcon == null)
					return;

				var actor = tooltipIcon.Actor;
				if (actor == null)
					return;

				var hotkey = tooltipIcon.Hotkey != null ? tooltipIcon.Hotkey.GetValue() : Hotkey.Invalid;
				if (actor == lastActor && hotkey == lastHotkey)
					return;

				var tooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(Exts.IsTraitEnabled);
				var name = tooltip != null ? tooltip.Name : actor.Name;
				var buildable = actor.TraitInfo<BuildableInfo>();
				var cost = actor.TraitInfo<ValuedInfo>().Cost;

				nameLabel.GetText = () => name;

				var nameWidth = font.Measure(name).X;
				var hotkeyWidth = 0;
				hotkeyLabel.Visible = hotkey.IsValid();

				if (hotkeyLabel.Visible)
				{
					var hotkeyText = "({0})".F(hotkey.DisplayString());

					hotkeyWidth = font.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X;
					hotkeyLabel.Text = hotkeyText;
					hotkeyLabel.Bounds.X = nameWidth + 2 * nameLabel.Bounds.X;
				}

				var prereqs = buildable.Prerequisites.Select(a => ActorName(mapRules, a)).Where(s => !s.StartsWith("~"));
				var requiresString = prereqs.Any() ? requiresLabel.Text.F(prereqs.JoinWith(", ")) : "";
				requiresLabel.GetText = () => requiresString;

				var power = actor.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(i => i.Amount);
				var powerString = power.ToString();
				powerLabel.GetText = () => powerString;
				powerLabel.GetColor = () => ((pm.PowerProvided - pm.PowerDrained) >= -power || power > 0)
					? Color.White : Color.Red;
				powerLabel.IsVisible = () => power != 0;
				powerIcon.IsVisible = () => power != 0;

				var lowpower = pm.PowerState != PowerState.Normal;
				var time = tooltipIcon.ProductionQueue == null ? 0 : tooltipIcon.ProductionQueue.GetBuildTime(actor, buildable)
					* (lowpower ? tooltipIcon.ProductionQueue.Info.LowPowerSlowdown : 1);
				var timeString = WidgetUtils.FormatTime(time, world.Timestep);
				timeLabel.GetText = () => timeString;
				timeLabel.GetColor = () => lowpower ? Color.Red : Color.White;

				var costString = cost.ToString();
				costLabel.GetText = () => costString;
				costLabel.GetColor = () => pr.Cash + pr.Resources >= cost
					? Color.White : Color.Red;

				var descString = buildable.Description.Replace("\\n", "\n");
				descLabel.GetText = () => descString;

				var leftWidth = new[] { nameWidth + hotkeyWidth, requiresFont.Measure(requiresString).X, descFont.Measure(descString).X }.Aggregate(Math.Max);
				var rightWidth = new[] { font.Measure(powerString).X, font.Measure(timeString).X, font.Measure(costString).X }.Aggregate(Math.Max);

				timeIcon.Bounds.X = powerIcon.Bounds.X = costIcon.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
				timeLabel.Bounds.X = powerLabel.Bounds.X = costLabel.Bounds.X = timeIcon.Bounds.Right + iconMargin;
				widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X + timeIcon.Bounds.Width + iconMargin;

				var leftHeight = font.Measure(name).Y + requiresFont.Measure(requiresString).Y + descFont.Measure(descString).Y;
				var rightHeight = font.Measure(powerString).Y + font.Measure(timeString).Y + font.Measure(costString).Y;
				widget.Bounds.Height = Math.Max(leftHeight, rightHeight) * 3 / 2 + 3 * nameLabel.Bounds.Y;

				lastActor = actor;
				lastHotkey = hotkey;
			};
		}

		static string ActorName(Ruleset rules, string a)
		{
			ActorInfo ai;
			if (rules.Actors.TryGetValue(a.ToLowerInvariant(), out ai))
			{
				var actorTooltip = ai.TraitInfos<TooltipInfo>().FirstOrDefault(Exts.IsTraitEnabled);
				if (actorTooltip != null)
					return actorTooltip.Name;
			}

			return a;
		}
	}
}