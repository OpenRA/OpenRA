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

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ImportLegacyMapCommand : IUtilityCommand
	{
		public string Name { get { return "--map-import"; } }

		[Desc("FILENAME", "Convert a legacy INI/MPR map to the OpenRA format.")]
		public void Run(ModData modData, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			Game.ModData = modData;

			var rules = Game.ModData.RulesetCache.LoadDefaultRules();
			var map = LegacyMapImporter.Import(args[1], modData.Manifest.Mod.Id, rules, e => Console.WriteLine(e));
			var dest = Path.ChangeExtension(args[1], "oramap");
			map.Save(dest);
			Console.WriteLine(dest + " saved.");
		}
	}
}
