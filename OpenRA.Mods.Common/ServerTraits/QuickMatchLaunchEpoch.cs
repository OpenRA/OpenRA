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
using OpenRA.Server;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	/// <summary>
	/// Gated behind OPENRA_QM_COUNTDOWN=true; the launch epoch is 50 seconds.
	/// We only support 1v1 at the moment.
	/// </summary>
	public class QuickMatchLaunchEpoch : ServerTrait, ITick
	{
		[FluentReference("seconds")]
		const string AutoReady = "notification-quickmatch-auto-ready";

		[FluentReference("seconds")]
		const string ReadyDeadline = "notification-quickmatch-ready-deadline";

		[FluentReference("count")]
		const string Countdown = "notification-quickmatch-countdown";

		[FluentReference]
		const string CountdownAborted = "notification-quickmatch-countdown-aborted";

		enum Status { Idle, Counting, Expired }

		const int Seconds = 50;
		static readonly bool Enabled = Environment.GetEnvironmentVariable("OPENRA_QM_COUNTDOWN")?
			.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

		// Portable monotonic millisecond clock 
		static long NowMs => System.Diagnostics.Stopwatch.GetTimestamp() / (System.Diagnostics.Stopwatch.Frequency / 1000);

		Status status;
		long deadline; // deadline in NowMs clock
		int nextStep; // the next ten-second announcement still to make

		public void Tick(S server)
		{
			if (!Enabled || server.Type != ServerType.Dedicated)
				return;

			lock (server.LobbyInfo)
			{
				if (server.State != ServerState.WaitingPlayers)
				{
					status = Status.Idle;
					return;
				}

				if (server.LobbyInfo.NonBotPlayers.Count() != 2)
				{
					if (status == Status.Counting)
					{
						Announce(server, "abort", 0);
						server.SendFluentMessage(CountdownAborted);
					}

					status = Status.Idle;
					return;
				}

				if (status == Status.Idle)
				{
					status = Status.Counting;
					deadline = NowMs + Seconds * 1000L;
					nextStep = Math.Min(5, Seconds / 10);
					Announce(server, "start", Seconds * 1000L);
					server.SendFluentMessage(AutoReady, "seconds", Seconds);
					server.SendFluentMessage(ReadyDeadline, "seconds", Seconds);
				}

				if (status != Status.Counting)
					return;

				var remaining = deadline - NowMs;
				while (nextStep > 0 && remaining <= nextStep * 10000L)
				{
					Announce(server, nextStep.ToStringInvariant(), remaining);
					server.SendFluentMessage(Countdown, "count", nextStep--);
				}

				if (remaining <= 0)
				{
					foreach (var conn in server.Conns.ToList())
					{
						var client = server.GetClient(conn);
						if (client == null || client.Bot != null || client.IsReady ||
							// We can only start with an unready observer if they are not an admin.
							// This is special case probably introduced by engine authors for tournament play.
							(client.IsObserver && !client.IsAdmin))
							continue;

						server.SendOrderTo(conn, "ServerError", "notification-you-were-kicked");
						server.DropClient(conn);
					}

					server.SyncLobbyClients();
					server.SyncLobbySlots();
					status = Status.Expired;
				}
			}
		}

		static void Announce(S server, string what, long remaining)
		{
			var order = Order.FromTargetString("QMCountdown", what, true);
			// We don't need precise syncing here, so we accept network lag in timing of "remaining".
			// Eventually all the clients ready up and we use normal engine routines to start the game.
			order.ExtraData = (uint)Math.Max(remaining, 0);
			server.DispatchServerOrdersToClients(order);
		}
	}
}
