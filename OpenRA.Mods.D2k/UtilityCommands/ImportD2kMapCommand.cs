﻿#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.IO;

namespace OpenRA.Mods.D2k.UtilityCommands
{
	class ImportD2kMapCommand : IUtilityCommand
	{
		public string Name { get { return "--import-d2k-map"; } }

		[Desc("FILENAME", "TILESET", "Convert a legacy Dune 2000 MAP file to the OpenRA format.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var rules = Game.ModData.RulesetCache.LoadDefaultRules();

			var map = D2kMapImporter.Import(args[1], modData.Manifest.Mod.Id, args[2], rules);

			if (map == null)
				return;

			var fileName = Path.GetFileNameWithoutExtension(args[1]);
			var dest = fileName + ".oramap";
			map.Save(dest);
			Console.WriteLine(dest + " saved.");
		}
	}
}
