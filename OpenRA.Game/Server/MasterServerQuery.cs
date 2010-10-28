#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenRA.FileFormats;
using OpenRA.Widgets;

namespace OpenRA.Server
{
    public static class MasterServerQuery
	{
		public static event Action<GameServer[]> OnComplete = _ => { };
		public static event Action<string> OnVersion = _ => { };

		static GameServer[] Games = { };
		public static string ClientVersion = "";
		public static string ServerVersion = "";
		static AutoResetEvent ev = new AutoResetEvent(false);
		static AutoResetEvent ev2 = new AutoResetEvent(false);

		public static void Refresh(string masterServerUrl)
		{
			new Thread(() =>
			{
				try
				{
					var str = GetData(new Uri(masterServerUrl + "list.php"));

					var yaml = MiniYaml.FromString(str);

					Games = yaml.Select(a => FieldLoader.Load<GameServer>(a.Value))
						.Where(gs => gs.Address != null).ToArray();
				}
				catch
				{
					Games = null;
				}

				ev.Set();
			}).Start();
		}

		public static void GetMOTD(string masterServerUrl)
		{
			var motd = Widget.RootWidget.GetWidget<ScrollingTextWidget>("MOTD_SCROLLER");
			
			// Runs in a separate thread to prevent dns lookup hitches
			new Thread(() =>
			{
				if (motd != null)
				{
					try
					{
						string motdText = GetData(new Uri(masterServerUrl + "motd.php?v=" + ClientVersion));
						string[] p = motdText.Split('|');
						if (p.Length == 2 && p[1].Length == int.Parse(p[0]))
						{
							motd.SetText(p[1]);
							motd.ResetScroll();
						}
					}
					catch
					{
						motd.SetText("Welcome to OpenRA. MOTD unable to be loaded from server.");
						motd.ResetScroll();
					}
				}

				ev.Set();
			}).Start();
		}

		public static void Tick()
		{
			if (ev.WaitOne(TimeSpan.FromMilliseconds(0))) 
				OnComplete(Games);
			if (ev2.WaitOne(TimeSpan.FromMilliseconds(0)))
				OnVersion(ServerVersion);
		}

		static string GetData(Uri uri)
		{
			var wc = new WebClient();
			var data = wc.DownloadData(uri);
			return Encoding.UTF8.GetString(data);
		}

		public static void GetCurrentVersion(string masterServerUrl)
		{
			new Thread(() =>
				{
					try
					{
						ServerVersion = GetData(new Uri(masterServerUrl + "VERSION"));
					}
					catch
					{
						ServerVersion = "";
					}

					ev2.Set();
				}).Start();
		}
	}

    public class GameServer
	{
		public readonly int Id = 0;
		public readonly string Name = null;
		public readonly string Address = null;
		public readonly int State = 0;
		public readonly int Players = 0;
		public readonly string Map = null;
		public readonly string[] Mods = { };
		public readonly int TTL = 0;
	}
}
