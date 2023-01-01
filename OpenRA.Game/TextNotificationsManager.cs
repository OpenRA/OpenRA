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
using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA
{
	public static class TextNotificationsManager
	{
		public static readonly int SystemClientId = -1;
		static readonly string SystemMessageLabel;

		public static long ChatDisabledUntil { get; internal set; }
		public static readonly Dictionary<int, bool> MutedPlayers = new Dictionary<int, bool>();

		static readonly List<TextNotification> NotificationsCache = new List<TextNotification>();
		public static IReadOnlyList<TextNotification> Notifications => NotificationsCache;

		static TextNotificationsManager()
		{
			if (!ChromeMetrics.TryGet("SystemMessageLabel", out SystemMessageLabel))
				SystemMessageLabel = "Battlefield Control";
		}

		public static void AddTransientLine(string text, Player player)
		{
			if (string.IsNullOrEmpty(text))
				return;

			if (player == null || player == player.World.LocalPlayer)
				AddTextNotification(TextNotificationPool.Transients, SystemClientId, SystemMessageLabel, text);
		}

		public static void AddFeedbackLine(string text)
		{
			AddTextNotification(TextNotificationPool.Feedback, SystemClientId, SystemMessageLabel, text);
		}

		public static void AddMissionLine(string prefix, string text, Color? prefixColor = null)
		{
			AddTextNotification(TextNotificationPool.Mission, SystemClientId, prefix, text, prefixColor);
		}

		public static void AddSystemLine(string text)
		{
			AddSystemLine(SystemMessageLabel, text);
		}

		public static void AddSystemLine(string prefix, string text)
		{
			AddTextNotification(TextNotificationPool.System, SystemClientId, prefix, text);
		}

		public static void AddChatLine(int clientId, string prefix, string text, Color? prefixColor = null, Color? textColor = null)
		{
			AddTextNotification(TextNotificationPool.Chat, clientId, prefix, text, prefixColor, textColor);
		}

		public static void Debug(string s, params object[] args)
		{
			AddSystemLine("Debug", string.Format(s, args));
		}

		static void AddTextNotification(TextNotificationPool pool, int clientId, string prefix, string text, Color? prefixColor = null, Color? textColor = null)
		{
			if (IsPoolEnabled(pool))
			{
				var textNotification = new TextNotification(pool, clientId, prefix, text, prefixColor, textColor);

				NotificationsCache.Add(textNotification);
				Ui.Send(textNotification);
			}
		}

		static bool IsPoolEnabled(TextNotificationPool pool)
		{
			var filters = Game.Settings.Game.TextNotificationPoolFilters;

			return pool == TextNotificationPool.Chat ||
				pool == TextNotificationPool.System ||
				pool == TextNotificationPool.Mission ||
				(pool == TextNotificationPool.Transients && filters.HasFlag(TextNotificationPoolFilters.Transients)) ||
				(pool == TextNotificationPool.Feedback && filters.HasFlag(TextNotificationPoolFilters.Feedback));
		}

		public static void Clear()
		{
			ChatDisabledUntil = Game.RunTime;
			NotificationsCache.Clear();
			MutedPlayers.Clear();
		}
	}
}
