#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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

namespace OpenRA.Mods.Common.UtilityCommands
{
	sealed class ExtractFilesCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--extract";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("FILENAME", "[FILENAME...]", "Extract files from mod packages to the current directory")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var files = args.Skip(1);
			var failed = new List<string>();

			foreach (var f in files)
			{
				try
				{
					var src = utility.ModData.DefaultFileSystem.Open(f);
					if (src == null)
					{
						failed.Add(f);
						continue;
					}

					var data = src.ReadAllBytes();
					File.WriteAllBytes(f, data);
					Console.WriteLine(f + " saved.");
				}
				catch (FileNotFoundException)
				{
					failed.Add(f);
				}
			}

			if (failed.Count > 0)
			{
				Console.Error.WriteLine($"{failed.Count} file(s) not found:");
				foreach (var f in failed)
					Console.Error.WriteLine($"  {f}");
				Environment.Exit(1);
			}
		}
	}
}
