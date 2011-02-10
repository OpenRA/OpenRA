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
using System.Globalization;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using OpenRA.FileFormats;

namespace OpenRA.Editor
{
	static class Program
	{
		[STAThread]
		static void Main( string[] args )
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
			var MapFolderPath = new string[] { Environment.CurrentDirectory, "mods", mod, "maps" }
				.Aggregate(Path.Combine);
			
			foreach (var path in ModData.FindMapsIn(MapFolderPath))
            {
                var map = new Map(path);
				map.Save(path);
            }
		}
		
	}
}
