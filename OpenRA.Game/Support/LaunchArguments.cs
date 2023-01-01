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
using OpenRA.Network;

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

		[Desc("Dump performance data into cpu.csv and render.csv in the logs folder with the given prefix.")]
		public string Benchmark;

		[Desc("Automatically start playing the given map.")]
		public string Map;

		public LaunchArguments(Arguments args)
		{
			if (args == null)
				return;

			foreach (var f in GetType().GetFields())
				if (args.Contains("Launch" + "." + f.Name))
					FieldLoader.LoadField(this, f.Name, args.GetValue("Launch" + "." + f.Name, ""));
		}

		public ConnectionTarget GetConnectEndPoint()
		{
			try
			{
				Uri uri;
				if (!string.IsNullOrEmpty(URI))
					uri = new Uri(URI);
				else if (!string.IsNullOrEmpty(Connect))
					uri = new Uri("tcp://" + Connect);
				else
					return null;

				if (uri.IsAbsoluteUri)
					return new ConnectionTarget(uri.Host, uri.Port);
				else
					return null;
			}
			catch (Exception ex)
			{
				Log.Write("client", "Failed to parse Launch.URI or Launch.Connect: {0}", ex.Message);
				return null;
			}
		}
	}
}
