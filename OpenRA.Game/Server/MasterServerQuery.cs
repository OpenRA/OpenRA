#region Copyright & License Information
/*
 * Copyright 2007,2009,2010 Chris Forbes, Robert Pepperell, Matthew Bowra-Dean, Paul Chote, Alli Witheford.
 * This file is part of OpenRA.
 * 
 *  OpenRA is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 * 
 *  OpenRA is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 * 
 *  You should have received a copy of the GNU General Public License
 *  along with OpenRA.  If not, see <http://www.gnu.org/licenses/>.
 */
#endregion

using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using OpenRA.FileFormats;

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
