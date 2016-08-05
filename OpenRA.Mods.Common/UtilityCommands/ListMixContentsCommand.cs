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
using System.Linq;
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ListMixContents : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--list-mix"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 2;
		}

		[Desc("ARCHIVE.MIX", "Lists the content ranges for a mix file")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var filename = Path.GetFileName(args[1]);
			var path = Path.GetDirectoryName(args[1]);

			var fs = new FileSystem.FileSystem(utility.Mods);

			// Needed to access the global mix database
			fs.LoadFromManifest(utility.ModData.Manifest);

			fs.Mount(path, "parent");
			var package = new MixFile(fs, "parent|" + filename);

			foreach (var kv in package.Index.OrderBy(kv => kv.Value.Offset))
			{
				Console.WriteLine("{0}:", kv.Key);
				Console.WriteLine("\tOffset: {0}", kv.Value.Offset);
				Console.WriteLine("\tLength: {0}", kv.Value.Length);
			}
		}
	}
}
