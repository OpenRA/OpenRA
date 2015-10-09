#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class GenerateMinimapCommand : IUtilityCommand
	{
		public string Name { get { return "--map-preview"; } }

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("MAPFILE", "Render PNG minimap of specified oramap file.")]
		public void Run(ModData modData, string[] args)
		{
			Game.ModData = modData;
			var map = new Map(args[1]);

			modData.ModFiles.UnmountAll();
			foreach (var dir in Game.ModData.Manifest.Folders)
				modData.ModFiles.Mount(dir);

			var minimap = Minimap.RenderMapPreview(map.Rules.TileSets[map.Tileset], map, true);

			var dest = Path.GetFileNameWithoutExtension(args[1]) + ".png";
			minimap.Save(dest);
			Console.WriteLine(dest + " saved.");
		}
	}
}
