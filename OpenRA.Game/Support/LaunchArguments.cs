#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

namespace OpenRA
{
	public class LaunchArguments
	{
		[Desc("Connect to the following server given as IP:PORT on startup.")]
		public string Connect;

		[Desc("Connect to the unified resource identifier openra://IP:PORT on startup.")]
		public string URI;

		[Desc("Automatically start playing the given replay file.")]
		public string Replay;

		[Desc("Dump performance data into cpu.csv and render.csv in the logs folder.")]
		public bool Benchmark;

		public LaunchArguments(Arguments args)
		{
			if (args == null)
				return;

			foreach (var f in GetType().GetFields())
				if (args.Contains("Launch" + "." + f.Name))
					FieldLoader.LoadField(this, f.Name, args.GetValue("Launch" + "." + f.Name, ""));
		}

		public string GetConnectAddress()
		{
			var connect = string.Empty;

			if (!string.IsNullOrEmpty(Connect))
				connect = Connect;

			if (!string.IsNullOrEmpty(URI))
				connect = URI.Replace("openra://", "").TrimEnd('/');

			return connect;
		}
	}
}
