#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using OpenRA.FileFormats;

namespace OpenRA.Editor
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length >= 2 && args[0] == "--convert")
			{
				Game.modData = new ModData(args[1]);
				FileSystem.LoadFromManifest(Game.modData.Manifest);
				Rules.LoadRules(Game.modData.Manifest, new Map());
				UpgradeMaps(args[1]);
				return;
			}

			Application.CurrentCulture = CultureInfo.InvariantCulture;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new Form1(args));
		}

		static void UpgradeMaps(string mod)
		{
			var mapFolderPath = new string[] { Environment.CurrentDirectory, "mods", mod, "maps" }
				.Aggregate(Path.Combine);

			foreach (var path in ModData.FindMapsIn(mapFolderPath))
			{
				var map = new Map(path);

				// Touch the lazy bits to initialize them
				map.Actors.Force();
				map.Smudges.Force();
				map.MapTiles.Force();
				map.MapResources.Force();
				map.Save(path);
			}
		}
	}
}
