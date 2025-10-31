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

using OpenRA.Network;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class AnonymousProfileTooltipLogic : ChromeLogic
	{
		[ObjectCreator.UseCtor]
		public AnonymousProfileTooltipLogic(Widget widget, Session.Client client)
		{
			var nameLabel = widget.Get<LabelWidget>("NAME");
			var nameFont = Game.Renderer.Fonts[nameLabel.Font];
			widget.Bounds.Width = nameFont.Measure(nameLabel.GetText()).X + 2 * nameLabel.Bounds.Left;

			var locationLabel = widget.Get<LabelWidget>("LOCATION");
			var ipLabel = widget.Get<LabelWidget>("IP");
			var adminLabel = widget.Get("GAME_ADMIN");

			if (client.Location != null)
			{
				var locationFont = Game.Renderer.Fonts[locationLabel.Font];
				var locationWidth = widget.Bounds.Width - 2 * locationLabel.Bounds.X;
				var location = WidgetUtils.TruncateText(client.Location, locationWidth, locationFont);
				locationLabel.IsVisible = () => true;
				locationLabel.GetText = () => location;
				widget.Bounds.Height += locationLabel.Bounds.Height;
				ipLabel.Bounds.Y += locationLabel.Bounds.Height;
				adminLabel.Bounds.Y += locationLabel.Bounds.Height;
			}

			if (client.AnonymizedIPAddress != null)
			{
				ipLabel.IsVisible = () => true;
				ipLabel.GetText = () => client.AnonymizedIPAddress;
				widget.Bounds.Height += ipLabel.Bounds.Height;
				adminLabel.Bounds.Y += locationLabel.Bounds.Height;
			}

			if (client.IsAdmin)
			{
				adminLabel.IsVisible = () => true;
				widget.Bounds.Height += adminLabel.Bounds.Height;
			}
		}
	}
}
