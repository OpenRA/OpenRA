#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenRA.FileFormats;

namespace OpenRA.Network
{
	public static class ServerList
	{
		public static void Query(Action<GameServer[]> onComplete)
		{
			var masterServerUrl = Game.Settings.Server.MasterServer;

			new Thread(() =>
			{
				GameServer[] games = null;
				try
				{
					var str = GetData(new Uri(masterServerUrl + "list.php"));

					var yaml = MiniYaml.FromString(str);

					games = yaml.Select(a => FieldLoader.Load<GameServer>(a.Value))
						.Where(gs => gs.Address != null).ToArray();

					foreach (var game in games)
						if (game.Latency < 0)
							game.Ping();
				}
				catch { }

				Game.RunAfterTick(() => onComplete(games));
			}) { IsBackground = true }.Start();
		}

		static string GetData(Uri uri)
		{
			var wc = new WebClient();
			wc.Proxy = null;
			var data = wc.DownloadData(uri);
			return Encoding.UTF8.GetString(data);
		}
	}
}
