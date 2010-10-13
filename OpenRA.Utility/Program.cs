using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;

namespace OpenRA.Utility
{
	static class Exts
	{
		public static string JoinString(this IEnumerable<string> self, string joiner)
		{
			if (self == null || self.Count() == 0) return "";
			StringBuilder sb = new StringBuilder();
			foreach (var s in self)
			{
				sb.Append(s);
				sb.Append(joiner);
			}
			sb.Remove(sb.Length - joiner.Length, joiner.Length);
			return sb.ToString();
		}
	}

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
			argCallbacks.Add("--list-mod-heirarchy", ListModHeirarchy);
			
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
			Console.WriteLine("  --mod-info=MOD          List metadata for MOD");
			Console.WriteLine("  --list-mod-heirarchy    Print a tree of the mod heirarchy");
		}

		static void ListMods(string argValue)
		{
			foreach (var m in Mod.AllMods.Where(x => !x.Key.StartsWith("Invalid mod:")).Select(x => x.Key))
				Console.WriteLine(m);
		}

		static void ListModInfo(string argValue)
		{
			var mod = Mod.AllMods
				.Where(x => x.Key.Equals(argValue))
				.Select(x => x.Value)
				.FirstOrDefault();
			if (mod == null)
			{
				Console.WriteLine("Error: Mod `{0}` is not installed or could not be found.", argValue);
				return;
			}

			Console.WriteLine("Title: {0}", mod.Title);
			Console.WriteLine("Version: {0}", mod.Version);
			Console.WriteLine("Author: {0}", mod.Author);
			Console.WriteLine("Description: {0}", mod.Description);
			Console.WriteLine("Requires: {0}", mod.RequiresMods.JoinString(","));
			Console.WriteLine("Standalone: {0}", mod.Standalone.ToString());
		}

		static void ListModHeirarchy(string argValue)
		{
			Console.WriteLine("TODO");
		}
	}
}
