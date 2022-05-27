#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.IO;
using System.Linq;
using OpenRA.Mods.Cnc.FileFormats;
using OpenRA.Mods.Cnc.FileSystem;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	class ListMixContents : IUtilityCommand
	{
		string IUtilityCommand.Name => "--list-mix";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 3;
		}

		[Desc("ARCHIVE.MIX", "MIXDATABASE.DAT", "Lists the content ranges for a mix file")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			string[] globalFilenames;
			using (var db = new XccGlobalDatabase(File.OpenRead(args[2])))
				globalFilenames = db.Entries;

			var package = new MixLoader.MixFile(File.OpenRead(args[1]), args[1], globalFilenames);
			foreach (var kv in package.Index.OrderBy(kv => kv.Value.Offset))
			{
				Console.WriteLine("{0}:", kv.Key);
				Console.WriteLine("\tOffset: {0}", kv.Value.Offset);
				Console.WriteLine("\tLength: {0}", kv.Value.Length);
			}
		}
	}
}
