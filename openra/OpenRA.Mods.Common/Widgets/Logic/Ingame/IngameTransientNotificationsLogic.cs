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

using System.Collections.Generic;
using OpenRA.Widgets;

namespace OpenRA.Mods.Common.Widgets.Logic
{
	public class IngameTransientNotificationsLogic : ChromeLogic, INotificationHandler<TextNotification>
	{
		readonly Ruleset modRules;

		readonly TextNotificationsDisplayWidget displayWidget;

		readonly string transientLineSound;

		TextNotification lastLine;
		int repetitions;

		[ObjectCreator.UseCtor]
		public IngameTransientNotificationsLogic(Widget widget, ModData modData, Dictionary<string, MiniYaml> logicArgs)
		{
			modRules = modData.DefaultRules;

			displayWidget = widget.Get<TextNotificationsDisplayWidget>("TRANSIENTS_DISPLAY");

			if (logicArgs.TryGetValue("TransientLineSound", out var yaml))
				transientLineSound = yaml.Value;
			else
				ChromeMetrics.TryGet("TransientLineSound", out transientLineSound);
		}

		void INotificationHandler<TextNotification>.Handle(TextNotification notification)
		{
			if (!IsNotificationEligible(notification))
				return;

			var lineToDisplay = notification;

			if (displayWidget.Children.Count > 0 && notification.CanIncrementOnDuplicate() && notification == lastLine)
			{
				repetitions++;
				lineToDisplay = new TextNotification(
					notification.Pool,
					notification.ClientId,
					notification.Prefix,
					$"{notification.Text} ({repetitions + 1})",
					notification.PrefixColor,
					notification.TextColor);

				displayWidget.RemoveMostRecentNotification();
			}
			else
				repetitions = 0;

			lastLine = notification;

			AddNotification(lineToDisplay);
		}

		void AddNotification(TextNotification notification, bool suppressSound = false)
		{
			displayWidget.AddNotification(notification);

			if (!suppressSound && !string.IsNullOrEmpty(transientLineSound))
				Game.Sound.PlayNotification(modRules, null, "Sounds", transientLineSound, null);
		}

		static bool IsNotificationEligible(TextNotification notification)
		{
			return notification.Pool == TextNotificationPool.Transients || notification.Pool == TextNotificationPool.Feedback;
		}
	}
}
