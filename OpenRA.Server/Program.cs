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

using System;
using System.IO;
using System.Net;
using System.Threading;
using OpenRA.Support;

namespace OpenRA.Server
{
	class Program
	{
		static void Main(string[] args)
		{
			var arguments = new Arguments(args);
			Log.AddChannel("debug", "dedicated-debug.log");
			Log.AddChannel("perf", "dedicated-perf.log");
			Log.AddChannel("server", "dedicated-server.log");
			Log.AddChannel("nat", "dedicated-nat.log");

			// Special case handling of Game.Mod argument: if it matches a real filesystem path
			// then we use this to override the mod search path, and replace it with the mod id
			var modArgument = arguments.GetValue("Game.Mod", null);
			string customModPath = null;
			if (modArgument != null && (File.Exists(modArgument) || Directory.Exists(modArgument)))
			{
				customModPath = modArgument;
				arguments.ReplaceValue("Game.Mod", Path.GetFileNameWithoutExtension(modArgument));
			}

			// HACK: The engine code assumes that Game.Settings is set.
			// This isn't nearly as bad as ModData, but is still not very nice.
			Game.InitializeSettings(arguments);
			var settings = Game.Settings.Server;

			var mod = Game.Settings.Game.Mod;
			var mods = new InstalledMods(customModPath);

			// HACK: The engine code *still* assumes that Game.ModData is set
			var modData = Game.ModData = new ModData(mods[mod], mods);
			modData.MapCache.LoadMaps();

			settings.Map = modData.MapCache.ChooseInitialMap(settings.Map, new MersenneTwister());

			Console.WriteLine("[{0}] Starting dedicated server for mod: {1}", DateTime.Now.ToString(settings.TimestampFormat), mod);
			while (true)
			{
				var server = new Server(new IPEndPoint(IPAddress.Any, settings.ListenPort), settings, modData, true);

				while (true)
				{
					Thread.Sleep(1000);
					if (server.State == ServerState.GameStarted && server.Conns.Count < 1)
					{
						Console.WriteLine("[{0}] No one is playing, shutting down...", DateTime.Now.ToString(settings.TimestampFormat));
						server.Shutdown();
						break;
					}
				}

				Console.WriteLine("[{0}] Starting a new server instance...", DateTime.Now.ToString(settings.TimestampFormat));
			}
		}
	}
}
