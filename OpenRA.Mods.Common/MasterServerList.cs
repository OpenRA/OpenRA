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
using System.Linq;
using System.Threading.Tasks;
using OpenRA.Network;
using OpenRA.Support;

namespace OpenRA.Mods.Common
{
	/// <summary>The games the "master" server is advertising for this mod.</summary>
	public static class MasterServerList
	{
		/// <summary>The query the server browser sends.</summary>
		public static string Query(ModData modData)
		{
			return new HttpQueryBuilder(modData.GetOrCreate<WebServices>().ServerList)
			{
				{ "protocol", GameServer.ProtocolVersion },
				{ "engine", Game.EngineVersion },
				{ "mod", modData.Manifest.Id },
				{ "version", modData.Manifest.Metadata.Version }
			}.ToString();
		}

		/// <summary>Maybe fetches the list asynchronously</summary>
		/// <remarks>We don't care to distinguish between the "master" server outage and no servers being currently advertised.</remarks>
		public static void Fetch(ModData modData, Action<List<GameServer>> then)
		{
			var url = Query(modData);
			Task.Run(async () =>
			{
				var games = new List<GameServer>();
				try
				{
					var client = HttpClientFactory.Create();
					var response = await client.GetAsync(url);
					var yaml = MiniYaml.FromStream(await response.Content.ReadAsStreamAsync(), url);
					games = yaml.Select(n => new GameServer(n.Value)).ToList();
				}
				catch (Exception e)
				{
					Log.Write("debug", $"Master server list failed to make fetch happen: {e.Message}");
				}

				Game.RunAfterTick(() => then(games));
			});
		}
	}
}
