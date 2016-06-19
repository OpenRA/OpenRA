#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using OpenRA.FileSystem;

namespace OpenRA.Mods.D2k.UtilityCommands
{
	class ImportD2kMapCommand : IUtilityCommand
	{
		public string Name { get { return "--import-d2k-map"; } }

		public bool ValidateArguments(string[] args)
		{
			return args.Length >= 3;
		}

		[Desc("FILENAME", "TILESET", "Convert a legacy Dune 2000 MAP file to the OpenRA format.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var rules = Ruleset.LoadDefaultsForTileSet(modData, "ARRAKIS");
			var map = D2kMapImporter.Import(args[1], modData.Manifest.Mod.Id, args[2], rules);

			if (map == null)
				return;

			var dest = Path.GetFileNameWithoutExtension(args[1]) + ".oramap";
			map.Save(ZipFile.Create(dest, new Folder(".")));
			Console.WriteLine(dest + " saved.");
		}
	}
}
