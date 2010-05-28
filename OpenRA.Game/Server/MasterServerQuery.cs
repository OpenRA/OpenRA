using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using OpenRA.FileFormats;
using System.Threading;

namespace OpenRA.Server
{
	static class MasterServerQuery
	{
		public static event Action<GameServer[]> OnComplete = _ => { };

		static GameServer[] Games = { };
		static AutoResetEvent ev = new AutoResetEvent(false);

		public static void Refresh(string masterServerUrl)
		{
			new Thread(() =>
			{
				try
				{
					var wc = new WebClient();
					var data = wc.DownloadData(new Uri(masterServerUrl + "list.php"));
					var str = Encoding.UTF8.GetString(data);

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

		public static void Tick()
		{
			if (ev.WaitOne(TimeSpan.FromMilliseconds(0))) 
				OnComplete(Games);
		}
	}

	class GameServer
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
