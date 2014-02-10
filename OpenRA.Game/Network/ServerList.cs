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
		public static string BestMasterServer;

		public static void ChooseMasterServer()
		{
			foreach (var masterServer in Game.Settings.Server.MasterServers)
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(masterServer + "list.php");
				request.Timeout = 2000;
				try
				{
					var response = (HttpWebResponse)request.GetResponse();
					if (response.StatusCode == HttpStatusCode.OK && string.IsNullOrEmpty(BestMasterServer))
					{
						BestMasterServer = masterServer;
						Log.Write("server", "Chose {0} as master server.", (BestMasterServer));
					}
				}
				catch (Exception) { }
			}
		}

		public static void Query(Action<GameServer[]> onComplete)
		{
			new Thread(() =>
			{
				GameServer[] games = null;
				try
				{
					var str = GetData(new Uri(BestMasterServer + "list.php"));

					var yaml = MiniYaml.FromString(str);

					games = yaml.Select(a => FieldLoader.Load<GameServer>(a.Value))
						.Where(gs => gs.Address != null).ToArray();
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
