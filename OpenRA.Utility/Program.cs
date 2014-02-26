#region Copyright & License Information
/*
 * Copyright 2007-2012 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using OpenRA.FileFormats;

namespace OpenRA.Utility
{
	class Program
	{
		static Dictionary<string, Action<string[]>> Actions = new Dictionary<string, Action<string[]>>()
		{
			{ "--settings-value", Command.Settings },
			{ "--shp", Command.ConvertPngToShp },
			{ "--png", Command.ConvertSpriteToPng },
			{ "--extract", Command.ExtractFiles },
			{ "--remap", Command.RemapShp },
			{ "--transpose", Command.TransposeShp },
			{ "--docs", Command.ExtractTraitDocs },
			{ "--map-hash", Command.GetMapHash },
			{ "--map-preview", Command.GenerateMinimap },
			{ "--map-upgrade-v5", Command.UpgradeV5Map },
			{ "--upgrade-map", UpgradeRules.UpgradeMap },
			{ "--upgrade-mod", UpgradeRules.UpgradeMod },
			{ "--map-import", Command.ImportLegacyMap },
			{ "--stats", Command.GenerateStats }
		};

		static void Main(string[] args)
		{
			if (args.Length == 0) { PrintUsage(); return; }

			AppDomain.CurrentDomain.AssemblyResolve += FileSystem.ResolveAssembly;

			Log.LogPath = Platform.SupportDir + "Logs" + Path.DirectorySeparatorChar;

			try
			{
				var action = Exts.WithDefault(_ => PrintUsage(), () => Actions[args[0]]);
				action(args);
			}
			catch (Exception e)
			{
				Log.AddChannel("utility", "utility.log");
				Log.Write("utility", "Received args: {0}", args.JoinWith(" "));
				Log.Write("utility", "{0}", e);

				Console.WriteLine("Error: Utility application crashed. See utility.log for details");
				throw;
			}
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: OpenRA.Utility.exe [OPTION] [ARGS]");
			Console.WriteLine();
			foreach (var a in Actions)
			{
				var descParts = a.Value.Method.GetCustomAttributes<DescAttribute>(true)
					.SelectMany(d => d.Lines);

				if (!descParts.Any())
					continue;

				var args = descParts.Take(descParts.Count() - 1).JoinWith(" ");
				var desc = descParts.Last();

				Console.WriteLine("  {0} {1}    ({2})", a.Key, args, desc);
			}
		}

		static string GetNamedArg(string[] args, string arg)
		{
			if (args.Length < 2)
				return null;

			var i = Array.IndexOf(args, arg);
			if (i < 0 || i == args.Length - 1)  // doesnt exist, or doesnt have a value.
				return null;

			return args[i + 1];
		}
	}
}
