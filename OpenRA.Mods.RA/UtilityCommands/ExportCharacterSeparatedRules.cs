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
using System.IO;

namespace OpenRA.Mods.RA.UtilityCommands
{
	class ExportCharacterSeparatedRules : IUtilityCommand
	{
		public string Name { get { return "--generate-dps-table"; } }

		[Desc("Export the damage per second evaluation into a CSV file for inspection.")]
		public void Run(ModData modData, string[] args)
		{
			Game.modData = modData;
			var table = ActorStatsExport.GenerateTable();
			var filename = "{0}-mod-dps.csv".F(Game.modData.Manifest.Mod.Id);
			using (var outfile = new StreamWriter(filename))
				outfile.Write(table.ToCharacterSeparatedValues(";", true));
			Console.WriteLine("{0} has been saved.".F(filename));
			Console.WriteLine("Open as values separated by semicolon.");
		}
	}
}
