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

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ListInstallShieldContents : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--list-installshield"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 2;
		}

		[Desc("ARCHIVE.Z", "Lists the content ranges for a InstallShield V3 file")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var filename = Path.GetFileName(args[1]);
			var path = Path.GetDirectoryName(args[1]);

			var fs = new FileSystem.FileSystem(utility.Mods);
			fs.Mount(path, "parent");
			var package = new InstallShieldPackage(fs, "parent|" + filename);

			foreach (var kv in package.Index)
			{
				Console.WriteLine("{0}:", kv.Key);
				Console.WriteLine("\tOffset: {0}", 255 + kv.Value.Offset);
				Console.WriteLine("\tLength: {0}", kv.Value.Length);
			}
		}
	}
}
