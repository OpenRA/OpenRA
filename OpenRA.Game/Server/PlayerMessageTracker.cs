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
using System.Collections.Generic;

namespace OpenRA.Server
{
	class PlayerMessageTracker
	{
		[TranslationReference("remaining")]
		const string ChatTemporaryDisabled = "notification-chat-temp-disabled";

		readonly Dictionary<int, List<long>> messageTracker = new Dictionary<int, List<long>>();
		readonly Server server;
		readonly Action<Connection, int, int, byte[]> dispatchOrdersToClient;
		readonly Action<Connection, string, Dictionary<string, object>> sendLocalizedMessageTo;

		public PlayerMessageTracker(Server server, Action<Connection, int, int, byte[]> dispatchOrdersToClient, Action<Connection, string, Dictionary<string, object>> sendLocalizedMessageTo)
		{
			this.server = server;
			this.dispatchOrdersToClient = dispatchOrdersToClient;
			this.sendLocalizedMessageTo = sendLocalizedMessageTo;
		}

		public void DisableChatUI(Connection conn, int time)
		{
			dispatchOrdersToClient(conn, 0, 0, new Order("DisableChatEntry", null, false) { ExtraData = (uint)time }.Serialize());
		}

		public bool IsPlayerAtFloodLimit(Connection conn)
		{
			if (!messageTracker.ContainsKey(conn.PlayerIndex))
				messageTracker.Add(conn.PlayerIndex, new List<long>());

			var isAdmin = server.GetClient(conn)?.IsAdmin ?? false;
			var settings = server.Settings;
			var time = conn.ConnectionTimer.ElapsedMilliseconds;
			var tracker = messageTracker[conn.PlayerIndex];
			tracker.RemoveAll(t => t + settings.FloodLimitInterval < time);

			long CalculateRemaining(long cooldown) => (cooldown - time + 999) / 1000;

			// Block messages until join cooldown times out
			if (!isAdmin && time < settings.FloodLimitJoinCooldown)
			{
				var remaining = CalculateRemaining(settings.FloodLimitJoinCooldown);
				sendLocalizedMessageTo(conn, ChatTemporaryDisabled, Translation.Arguments("remaining", remaining));
				return true;
			}

			// Block messages if above flood limit
			if (tracker.Count >= settings.FloodLimitMessageCount)
			{
				var remaining = CalculateRemaining(tracker[0] + settings.FloodLimitInterval);
				sendLocalizedMessageTo(conn, ChatTemporaryDisabled, Translation.Arguments("remaining", remaining));
				return true;
			}

			tracker.Add(time);

			// Disable chat when player has reached the flood limit
			if (tracker.Count >= settings.FloodLimitMessageCount)
			{
				var cooldownDelta = Math.Max(0, settings.FloodLimitCooldown - settings.FloodLimitInterval);
				for (var i = 0; i < tracker.Count; i++)
					tracker[i] = time + cooldownDelta;

				DisableChatUI(conn, settings.FloodLimitCooldown);
			}

			return false;
		}
	}
}
