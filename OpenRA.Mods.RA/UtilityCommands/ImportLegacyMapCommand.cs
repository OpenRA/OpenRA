#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.RA.UtilityCommands
{
	class ImportLegacyMapCommand : IUtilityCommand
	{
		public string Name { get { return "--map-import"; } }

		[Desc("MOD", "FILENAME", "Convert a legacy INI/MPR map to the OpenRA format.")]
		public void Run(string[] args)
		{
			var mod = args[1];
			var filename = args[2];
			Game.modData = new ModData(mod);
			var rules = Game.modData.RulesetCache.LoadDefaultRules();
			var map = LegacyMapImporter.Import(filename, mod, rules, e => Console.WriteLine(e));
			var dest = map.Title + ".oramap";
			map.Save(dest);
			Console.WriteLine(dest + " saved.");
		}
	}
}
