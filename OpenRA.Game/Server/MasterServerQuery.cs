using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using OpenRA.FileFormats;

namespace OpenRA.Server
{
	static class MasterServerQuery
	{
		public static IEnumerable<GameServer> GetGameList(string masterServerUrl)
		{
			var wc = new WebClient();
			var data = wc.DownloadData(masterServerUrl + "list.php");
			var str = Encoding.UTF8.GetString(data);

			var yaml = MiniYaml.FromString(str);
			return yaml.Select(a => { var gs = new GameServer(); FieldLoader.Load(gs, a.Value); return gs; })
				.Where(gs => gs.Address != null);
		}
	}

	class GameServer
	{
		public readonly string Name = null;
		public readonly string Address = null;
		public readonly int State = 0;
		public readonly int Players = 0;
		public readonly string Map = null;
		public readonly string[] Mods = { };
		public readonly int TTL = 0;
	}
}
