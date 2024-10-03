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
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class PlayerProfileBadgesLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public PlayerProfileBadgesLogic(Widget widget, PlayerProfile profile, Func<int, int> negotiateWidth)
		{
			var showBadges = profile.Badges.Count > 0;
			widget.IsVisible = () => showBadges;

			var badgeTemplate = widget.Get("BADGE_TEMPLATE");
			widget.RemoveChild(badgeTemplate);

			// Negotiate the label length that the tooltip will allow
			var maxLabelWidth = 0;
			var templateIcon = badgeTemplate.Get("ICON");
			var templateLabel = badgeTemplate.Get<LabelWidget>("LABEL");
			var templateLabelFont = Game.Renderer.Fonts[templateLabel.Font];
			foreach (var badge in profile.Badges)
				maxLabelWidth = Math.Max(maxLabelWidth, templateLabelFont.Measure(badge.Label).X);

			widget.Bounds.Width = negotiateWidth(2 * templateLabel.Bounds.Left - templateIcon.Bounds.Right + maxLabelWidth);

			var badgeOffset = badgeTemplate.Bounds.Y;
			if (profile.Badges.Count > 0)
				badgeOffset += 3;

			foreach (var badge in profile.Badges)
			{
				var b = badgeTemplate.Clone();
				var icon = b.Get<BadgeWidget>("ICON");
				icon.Badge = badge;

				var label = b.Get<LabelWidget>("LABEL");
				var labelFont = Game.Renderer.Fonts[label.Font];

				var labelText = WidgetUtils.TruncateText(badge.Label, widget.Bounds.Width - label.Bounds.X - icon.Bounds.X, labelFont);
				label.GetText = () => labelText;

				b.Bounds.Y = badgeOffset;
				widget.AddChild(b);

				badgeOffset += badgeTemplate.Bounds.Height;
			}

			if (badgeOffset > badgeTemplate.Bounds.Y)
				badgeOffset += 5;

			widget.Bounds.Height = badgeOffset;
		}
	}
}
