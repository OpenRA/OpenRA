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

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenRA.Network;
using OpenRA.Server;
using OpenRA.Support;
using S = OpenRA.Server.Server;

namespace OpenRA.Mods.Common.Server
{
	public class QueryRegisteredClientTooltip : IGlobalModData
	{
		[FieldLoader.Require]
		public readonly string Endpoint = null;
	}

	public class AddRegisteredClientTooltip : ServerTrait, IClientAuthenticated
	{
		class PlayerInfo
		{
			public readonly string Line1 = null;
			public readonly string Line2 = null;
			public readonly string Line3 = null;
		}

		void IClientAuthenticated.ClientAuthenticated(S server, Session.Client client, PlayerProfile profile)
		{
			Task.Run(async () =>
			{
				var httpClient = HttpClientFactory.Create();

				var queryUrl = server.ModData.Manifest.Get<QueryRegisteredClientTooltip>().Endpoint + profile.ProfileID;
				var httpResponseMessage = await httpClient.GetAsync(queryUrl);

				if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
				{
					Log.Write("server", "Failed to query tooltip info for profile {0}; HTTP status: {1}",
						profile.ProfileID, httpResponseMessage.StatusCode);
					return;
				}

				var data = await httpResponseMessage.Content.ReadAsStringAsync();
				try
				{
					var yaml = MiniYaml.FromString(data).First();
					if (yaml.Key != "ServerInfo")
					{
						Log.Write("server", "Failed to query tooltip info for profile {0}; unexpected data:\n{1}",
							profile.ProfileID, data);
						return;
					}

					var info = FieldLoader.Load<PlayerInfo>(yaml.Value);

					// The client info is stored inside the LobbyInfo, so we must take the lock to avoid race conditions
					lock (server.LobbyInfo)
					{
						client.ServerTooltipLine1 = info.Line1;
						client.ServerTooltipLine2 = info.Line2;
						client.ServerTooltipLine3 = info.Line3;
						server.SyncLobbyClients();
					}
				}
				catch (Exception e)
				{
					Log.Write("server", "Failed to query tooltip info for profile {0}; exception: {1}", profile.ProfileID, e);
				}
			});
		}
	}
}
