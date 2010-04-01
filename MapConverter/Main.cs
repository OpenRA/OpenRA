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

			var converter = new MapConverter(args[1]);
			converter.Map.DebugContents();
			converter.Save(args[2]);
		}
	}
}
