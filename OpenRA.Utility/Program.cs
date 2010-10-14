using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;

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
			Console.WriteLine("  --list-mods             List currently installed mods");
			Console.WriteLine("  --mod-info=MODS         List metadata for MODS (comma separated list of mods)");
		}

		static void ListMods(string argValue)
		{
			foreach (var m in Mod.AllMods.Where(x => !x.Key.StartsWith("Invalid mod:")).Select(x => x.Key))
				Console.WriteLine(m);
		}

		static void ListModInfo(string argValue)
		{
			string[] mods = argValue.Split(',');
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
	}
}
