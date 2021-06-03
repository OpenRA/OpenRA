#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using OpenRA.Primitives;
using OpenRA.Widgets;

namespace OpenRA
{
	public static class TextNotificationsManager
	{
		static Color systemMessageColor = Color.White;
		static Color chatMessageColor = Color.White;
		static string systemMessageLabel;

		static TextNotificationsManager()
		{
			ChromeMetrics.TryGet("ChatMessageColor", out chatMessageColor);
			ChromeMetrics.TryGet("SystemMessageColor", out systemMessageColor);
			if (!ChromeMetrics.TryGet("SystemMessageLabel", out systemMessageLabel))
				systemMessageLabel = "Battlefield Control";
		}

		public static void AddSystemLine(string text)
		{
			AddSystemLine(systemMessageLabel, text);
		}

		public static void AddSystemLine(string name, string text)
		{
			Game.OrderManager.AddChatLine(name, systemMessageColor, text, systemMessageColor);
		}

		public static void AddChatLine(string name, Color nameColor, string text)
		{
			Game.OrderManager.AddChatLine(name, nameColor, text, chatMessageColor);
		}

		public static void Debug(string s, params object[] args)
		{
			AddSystemLine("Debug", string.Format(s, args));
		}
	}
}
