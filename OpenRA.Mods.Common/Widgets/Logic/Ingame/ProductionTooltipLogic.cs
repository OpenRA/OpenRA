#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using OpenRA.Primitives;
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
			var pm = player.PlayerActor.TraitOrDefault<PowerManager>();
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
			var formatBuildTime = new CachedTransform<int, string>(time => WidgetUtils.FormatTime(time, world.Timestep));
			var requiresFormat = requiresLabel.Text;

			ActorInfo lastActor = null;
			var lastHotkey = Hotkey.Invalid;
			var lastPowerState = pm?.PowerState ?? PowerState.Normal;
			var descLabelY = descLabel.Bounds.Y;
			var descLabelPadding = descLabel.Bounds.Height;

			tooltipContainer.BeforeRender = () =>
			{
				var tooltipIcon = getTooltipIcon();

				var actor = tooltipIcon?.Actor;
				if (actor == null)
					return;

				var hotkey = tooltipIcon.Hotkey?.GetValue() ?? Hotkey.Invalid;
				if (actor == lastActor && hotkey == lastHotkey && (pm == null || pm.PowerState == lastPowerState))
					return;

				var tooltip = actor.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				var name = tooltip?.Name ?? actor.Name;
				var buildable = actor.TraitInfo<BuildableInfo>();

				var cost = 0;
				if (tooltipIcon.ProductionQueue != null)
					cost = tooltipIcon.ProductionQueue.GetProductionCost(actor);
				else
				{
					var valued = actor.TraitInfoOrDefault<ValuedInfo>();
					if (valued != null)
						cost = valued.Cost;
				}

				nameLabel.Text = name;

				var nameSize = font.Measure(name);
				var hotkeyWidth = 0;
				hotkeyLabel.Visible = hotkey.IsValid();

				if (hotkeyLabel.Visible)
				{
					var hotkeyText = $"({hotkey.DisplayString()})";

					hotkeyWidth = font.Measure(hotkeyText).X + 2 * nameLabel.Bounds.X;
					hotkeyLabel.Text = hotkeyText;
					hotkeyLabel.Bounds.X = nameSize.X + 2 * nameLabel.Bounds.X;
				}

				var prereqs = buildable.Prerequisites.Select(a => ActorName(mapRules, a))
					.Where(s => !s.StartsWith("~", StringComparison.Ordinal) && !s.StartsWith("!", StringComparison.Ordinal));

				var requiresSize = int2.Zero;
				if (prereqs.Any())
				{
					requiresLabel.Text = requiresFormat.F(prereqs.JoinWith(", "));
					requiresSize = requiresFont.Measure(requiresLabel.Text);
					requiresLabel.Visible = true;
					descLabel.Bounds.Y = descLabelY + requiresLabel.Bounds.Height;
				}
				else
				{
					requiresLabel.Visible = false;
					descLabel.Bounds.Y = descLabelY;
				}

				var powerSize = new int2(0, 0);
				if (pm != null)
				{
					var power = actor.TraitInfos<PowerInfo>().Where(i => i.EnabledByDefault).Sum(i => i.Amount);
					powerLabel.Text = power.ToString();
					powerLabel.GetColor = () => ((pm.PowerProvided - pm.PowerDrained) >= -power || power > 0)
						? Color.White : Color.Red;
					powerLabel.Visible = power != 0;
					powerIcon.Visible = power != 0;
					powerSize = font.Measure(powerLabel.Text);
				}

				var buildTime = tooltipIcon.ProductionQueue?.GetBuildTime(actor, buildable) ?? 0;
				var timeModifier = pm != null && pm.PowerState != PowerState.Normal ? tooltipIcon.ProductionQueue.Info.LowPowerModifier : 100;

				timeLabel.Text = formatBuildTime.Update((buildTime * timeModifier) / 100);
				timeLabel.TextColor = (pm != null && pm.PowerState != PowerState.Normal && tooltipIcon.ProductionQueue.Info.LowPowerModifier > 100) ? Color.Red : Color.White;
				var timeSize = font.Measure(timeLabel.Text);

				costLabel.Text = cost.ToString();
				costLabel.GetColor = () => pr.Cash + pr.Resources >= cost ? Color.White : Color.Red;
				var costSize = font.Measure(costLabel.Text);

				descLabel.Text = buildable.Description.Replace("\\n", "\n");
				var descSize = descFont.Measure(descLabel.Text);
				descLabel.Bounds.Width = descSize.X;
				descLabel.Bounds.Height = descSize.Y + descLabelPadding;

				var leftWidth = new[] { nameSize.X + hotkeyWidth, requiresSize.X, descSize.X }.Aggregate(Math.Max);
				var rightWidth = new[] { powerSize.X, timeSize.X, costSize.X }.Aggregate(Math.Max);

				timeIcon.Bounds.X = powerIcon.Bounds.X = costIcon.Bounds.X = leftWidth + 2 * nameLabel.Bounds.X;
				timeLabel.Bounds.X = powerLabel.Bounds.X = costLabel.Bounds.X = timeIcon.Bounds.Right + iconMargin;
				widget.Bounds.Width = leftWidth + rightWidth + 3 * nameLabel.Bounds.X + timeIcon.Bounds.Width + iconMargin;

				// Set the bottom margin to match the left margin
				var leftHeight = descLabel.Bounds.Bottom + descLabel.Bounds.X;

				// Set the bottom margin to match the top margin
				var rightHeight = (powerLabel.Visible ? powerIcon.Bounds.Bottom : timeIcon.Bounds.Bottom) + costIcon.Bounds.Top;

				widget.Bounds.Height = Math.Max(leftHeight, rightHeight);

				lastActor = actor;
				lastHotkey = hotkey;
				if (pm != null)
					lastPowerState = pm.PowerState;
			};
		}

		static string ActorName(Ruleset rules, string a)
		{
			if (rules.Actors.TryGetValue(a.ToLowerInvariant(), out var ai))
			{
				var actorTooltip = ai.TraitInfos<TooltipInfo>().FirstOrDefault(info => info.EnabledByDefault);
				if (actorTooltip != null)
					return actorTooltip.Name;
			}

			return a;
		}
	}
}
