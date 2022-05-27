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
using OpenRA.Mods.Common.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ListInstallShieldContents : IUtilityCommand
	{
		string IUtilityCommand.Name => "--list-installshield";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 2;
		}

		[Desc("ARCHIVE.Z", "Lists the content ranges for a InstallShield V3 file")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var package = new InstallShieldLoader.InstallShieldPackage(File.OpenRead(args[1]), args[1]);
			foreach (var kv in package.Index)
			{
				Console.WriteLine("{0}:", kv.Key);
				Console.WriteLine("\tOffset: {0}", 255 + kv.Value.Offset);
				Console.WriteLine("\tLength: {0}", kv.Value.Length);
			}
		}
	}
}
