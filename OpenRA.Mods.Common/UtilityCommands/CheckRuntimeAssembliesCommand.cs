#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class CheckRuntimeAssembliesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--check-runtime-assemblies"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("ASSEMBLY [ASSEMBLY ...]", "Check the runtime dependencies of the mod against a given whitelist of " +
				"assembly (dll and exe) names and generate an error if any unlisted files are required.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var whitelist = args
				.Skip(1)
				.Select(a => Path.GetFileName(a))
				.ToArray();

			// Load the renderer assembly so we can check its dependencies
			Assembly.LoadFile(Path.Combine(Platform.BinDir, "OpenRA.Platforms.Default.dll"));

			var missing = new List<string>();
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				var assemblyName = Path.GetFileName(a.Location);
				if (!whitelist.Contains(assemblyName))
					missing.Add(assemblyName);
			}

			if (missing.Any())
			{
				Console.WriteLine("error: The following assemblies are referenced but not whitelisted:");
				foreach (var m in missing)
					Console.WriteLine("   " + m);
				Environment.Exit(1);
			}
		}
	}
}
