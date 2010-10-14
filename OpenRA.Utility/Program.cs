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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using System.IO;

namespace OpenRA.Utility
{
	class Program
	{
		static KeyValuePair<string, string> SplitArgs(string arg)
		{
			int i = arg.IndexOf('=');
			if (i < 0) return new KeyValuePair<string, string>(arg, "");
			return new KeyValuePair<string, string>(arg.Substring(0, i), arg.Substring(i + 1));
		}

		delegate void ArgCallback(string argValue);

		static Dictionary<string, ArgCallback> argCallbacks;

		static void Main(string[] args)
		{
			argCallbacks = new Dictionary<string, ArgCallback>();
			argCallbacks.Add("--list-mods", ListMods);
			argCallbacks.Add("--mod-info", ListModInfo);
			argCallbacks.Add("--install-ra-music", InstallRAMusic);
			argCallbacks.Add("--install-cnc-music", InstallCncMusic);
			
			if (args.Length == 0) { PrintUsage(); return; }
			var arg = SplitArgs(args[0]);
			ArgCallback callback;
			if (argCallbacks.TryGetValue(arg.Key, out callback))
				callback(arg.Value);
			else
				PrintUsage();
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: OpenRA.Utility.exe [OPTION]");
			Console.WriteLine();
			Console.WriteLine("  --list-mods               List currently installed mods");
			Console.WriteLine("  --mod-info=MODS           List metadata for MODS (comma separated list of mods)");
			Console.WriteLine("  --install-ra-music=PATH   Install scores.mix from PATH to Red Alert CD");
			Console.WriteLine("  --install-cnc-music=PATH  Install scores.mix from PATH to Command & Conquer CD");
		}

		static void ListMods(string _)
		{
			foreach (var m in Mod.AllMods.Where(x => !x.Key.StartsWith("Invalid mod:")).Select(x => x.Key))
				Console.WriteLine(m);
		}

		static void ListModInfo(string modList)
		{
			string[] mods = modList.Split(',');
			foreach (var m in mods)
			{
				var mod = Mod.AllMods
					.Where(x => x.Key.Equals(m))
					.Select(x => x.Value)
					.FirstOrDefault();
				if (mod == null)
				{
					Console.WriteLine("Error: Mod `{0}` is not installed or could not be found.", m);
					return;
				}

				Console.WriteLine("{0}:", m);
				Console.WriteLine("  Title: {0}", mod.Title);
				Console.WriteLine("  Version: {0}", mod.Version);
				Console.WriteLine("  Author: {0}", mod.Author);
				Console.WriteLine("  Description: {0}", mod.Description);
				Console.WriteLine("  Requires: {0}", mod.RequiresMods == null ? "" : string.Join(",", mod.RequiresMods));
				Console.WriteLine("  Standalone: {0}", mod.Standalone.ToString());
			}
		}

		static void InstallRAMusic(string path)
		{
			if (!Directory.Exists(path)) { Console.WriteLine("Error: Path {0} does not exist", path); return; }
			FileSystem.Mount(path);
			if (!FileSystem.Exists("MAIN.MIX")) { Console.WriteLine("Error: Could not find MAIN.MIX in path {0}", path); return; }
			FileSystem.Mount("MAIN.MIX");

			using (var scoresStream = FileSystem.Open("scores.mix"))
				using (var destStream = File.Create(string.Format("mods{0}ra{0}packages{0}scores.mix", Path.DirectorySeparatorChar)))
					destStream.Write(scoresStream.ReadAllBytes());

			Console.WriteLine("Done");
		}

		static void InstallCncMusic(string path)
		{
			if (!Directory.Exists(path)) { Console.WriteLine("Error: Path {0} does not exist", path); return; }
			string scoresMixPath = path + Path.DirectorySeparatorChar + "SCORES.MIX";
			if (!File.Exists(scoresMixPath)) { Console.WriteLine("Error: Could not find SCORES.MIX in path {0}", path); return; }

			File.Copy(scoresMixPath, string.Format("mods{0}cnc{0}packages{0}scores.mix", Path.DirectorySeparatorChar), true);

			Console.WriteLine("Done");
		}
	}
}
