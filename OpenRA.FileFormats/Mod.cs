using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OpenRA.FileFormats
{
	public class Mod
	{
		public string Title;
		public string Description;
		public string Version;
		public string Author;
		public string[] RequiresMods;
		public bool Standalone = false;

		public static readonly Dictionary<string, Mod> AllMods = ValidateMods(Directory.GetDirectories("mods").Select(x => x.Substring(5)).ToArray());

		public static Dictionary<string, Mod> ValidateMods(string[] mods)
		{
			var ret = new Dictionary<string, Mod>();
			foreach (var m in mods)
			{
				if (!File.Exists("mods" + Path.DirectorySeparatorChar + m + Path.DirectorySeparatorChar + "mod.yaml"))
					continue;

				var yaml = new MiniYaml(null, MiniYaml.FromFile("mods" + Path.DirectorySeparatorChar + m + Path.DirectorySeparatorChar + "mod.yaml"));
				if (!yaml.NodesDict.ContainsKey("Metadata"))
				{
					System.Console.WriteLine("Invalid mod: " + m);
					continue;
				}

				ret.Add(m, FieldLoader.Load<Mod>(yaml.NodesDict["Metadata"]));
			}
			return ret;
		}
	}
}
