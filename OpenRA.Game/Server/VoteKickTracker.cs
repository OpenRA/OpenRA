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
using System.Diagnostics;
using OpenRA.Network;

namespace OpenRA.Server
{
	public sealed class VoteKickTracker
	{
		[TranslationReference("kickee")]
		const string InsufficientVotes = "notification-insufficient-votes-to-kick";

		[TranslationReference]
		const string AlreadyVoted = "notification-kick-already-voted";

		[TranslationReference("kicker", "kickee")]
		const string VoteKickStarted = "notification-vote-kick-started";

		[TranslationReference]
		const string UnableToStartAVote = "notification-unable-to-start-a-vote";

		[TranslationReference("kickee", "percentage")]
		const string VoteKickProgress = "notification-vote-kick-in-progress";

		[TranslationReference("kickee")]
		const string VoteKickEnded = "notification-vote-kick-ended";

		readonly Dictionary<int, bool> voteTracker = new();
		readonly Dictionary<Session.Client, long> failedVoteKickers = new();
		readonly Server server;

		Stopwatch voteKickTimer;
		(Session.Client Client, Connection Conn) kickee;
		(Session.Client Client, Connection Conn) voteKickerStarter;

		public VoteKickTracker(Server server)
		{
			this.server = server;
		}

		// Only admins and alive players can participate in a vote kick.
		bool ClientHasPower(Session.Client client) => client.IsAdmin || (!client.IsObserver && !server.HasClientWonOrLost(client));

		public void Tick()
		{
			if (voteKickTimer == null)
				return;

			if (!server.Conns.Contains(kickee.Conn))
			{
				EndKickVote();
				return;
			}

			if (voteKickTimer.ElapsedMilliseconds > server.Settings.VoteKickTimer)
				EndKickVoteAndBlockKicker();
		}

		public bool VoteKick(Connection conn, Session.Client kicker, Connection kickeeConn, Session.Client kickee, int kickeeID, bool vote)
		{
			var voteInProgress = voteKickTimer != null;

			if (server.State != ServerState.GameStarted
				|| (kickee.IsAdmin && server.Type != ServerType.Dedicated)
				|| (!voteInProgress && !vote) // Disallow starting a vote with a downvote
				|| (voteInProgress && this.kickee.Client != kickee) // Disallow starting new votes when one is already ongoing.
				|| !ClientHasPower(kicker))
			{
				server.SendLocalizedMessageTo(conn, UnableToStartAVote);
				return false;
			}

			short eligiblePlayers = 0;
			var isKickeeOnline = false;
			var adminIsDeadButOnline = false;
			foreach (var c in server.Conns)
			{
				var client = server.GetClient(c);
				if (client != kickee && ClientHasPower(client))
					eligiblePlayers++;

				if (c == kickeeConn)
					isKickeeOnline = true;

				if (client.IsAdmin && (client.IsObserver || server.HasClientWonOrLost(client)))
					adminIsDeadButOnline = true;
			}

			if (!isKickeeOnline)
			{
				EndKickVote();
				return false;
			}

			if (eligiblePlayers < 2 || (adminIsDeadButOnline && !kickee.IsAdmin && eligiblePlayers < 3))
			{
				if (!kickee.IsObserver && !server.HasClientWonOrLost(kickee))
				{
					// Vote kick cannot be the sole deciding factor for a game.
					server.SendLocalizedMessageTo(conn, InsufficientVotes, Translation.Arguments("kickee", kickee.Name));
					EndKickVote();
					return false;
				}
				else if (vote)
				{
					// If only a single player is playing, allow him to kick observers.
					EndKickVote(false);
					return true;
				}
			}

			if (!voteInProgress)
			{
				// Prevent vote kick spam abuse.
				if (failedVoteKickers.TryGetValue(kicker, out var time))
				{
					if (time + server.Settings.VoteKickerCooldown > kickeeConn.ConnectionTimer.ElapsedMilliseconds)
					{
						server.SendLocalizedMessageTo(conn, UnableToStartAVote);
						return false;
					}
					else
						failedVoteKickers.Remove(kicker);
				}

				Log.Write("server", $"Vote kick started on {kickeeID}.");
				voteKickTimer = Stopwatch.StartNew();
				server.SendLocalizedMessage(VoteKickStarted, Translation.Arguments("kicker", kicker.Name, "kickee", kickee.Name));
				server.DispatchServerOrdersToClients(new Order("StartKickVote", null, false) { ExtraData = (uint)kickeeID }.Serialize());
				this.kickee = (kickee, kickeeConn);
				voteKickerStarter = (kicker, conn);
			}

			if (!voteTracker.ContainsKey(conn.PlayerIndex))
				voteTracker[conn.PlayerIndex] = vote;
			else
			{
				server.SendLocalizedMessageTo(conn, AlreadyVoted, null);
				return false;
			}

			short votesFor = 0;
			short votesAgainst = 0;
			foreach (var c in voteTracker)
			{
				if (c.Value)
					votesFor++;
				else
					votesAgainst++;
			}

			// Include the kickee in eligeablePlayers, so that in a 2v2 or any other even team
			// matchup one team could not vote out the other team's player.
			if (ClientHasPower(kickee))
			{
				eligiblePlayers++;
				votesAgainst++;
			}

			var votesNeeded = eligiblePlayers / 2 + 1;
			server.SendLocalizedMessage(VoteKickProgress, Translation.Arguments(
				"kickee", kickee.Name,
				"percentage", votesFor * 100 / eligiblePlayers));

			// If a player or players during a vote lose or disconnect, it is possible that a downvote will
			// kick a client. Guard against that situation.
			if (vote && (votesFor >= votesNeeded))
			{
				EndKickVote(false);
				return true;
			}

			// End vote if it can never succeed.
			if (eligiblePlayers - votesAgainst < votesNeeded)
			{
				EndKickVoteAndBlockKicker();
				return false;
			}

			voteKickTimer.Restart();
			return false;
		}

		void EndKickVoteAndBlockKicker()
		{
			// Make sure vote kick is in progress.
			if (voteKickTimer == null)
				return;

			if (server.Conns.Contains(voteKickerStarter.Conn))
				failedVoteKickers[voteKickerStarter.Client] = voteKickerStarter.Conn.ConnectionTimer.ElapsedMilliseconds;

			EndKickVote();
		}

		void EndKickVote(bool sendMessage = true)
		{
			// Make sure vote kick is in progress.
			if (voteKickTimer == null)
				return;

			if (sendMessage)
				server.SendLocalizedMessage(VoteKickEnded, Translation.Arguments("kickee", kickee.Client.Name));

			server.DispatchServerOrdersToClients(new Order("EndKickVote", null, false) { ExtraData = (uint)kickee.Client.Index }.Serialize());

			voteKickTimer = null;
			voteKickerStarter = (null, null);
			kickee = (null, null);
			voteTracker.Clear();
		}
	}
}
