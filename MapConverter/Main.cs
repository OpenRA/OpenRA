using System;
using OpenRA.FileFormats;

namespace MapConverter
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if (args.Length != 3)
			{
				Console.WriteLine("usage: MapConverter mod[,mod]* input-map.ini output-map.yaml");
				return;
			}

			var mods = args[0].Split(',');
			var manifest = new Manifest(mods);

			foreach (var folder in manifest.Folders) FileSystem.Mount(folder);
			foreach (var pkg in manifest.Packages) FileSystem.Mount(pkg);

			var map = new NewMap(args[2]);
			map.DebugContents();
			
			//var map = new IniMap(args[1]);
			//map.Save(args[2]);
		}
	}
}
