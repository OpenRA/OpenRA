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
using System.IO;
using System.Linq;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractFilesCommand : IUtilityCommand
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

			foreach (var f in files)
			{
				var src = utility.ModData.DefaultFileSystem.Open(f);
				if (src == null)
					throw new InvalidOperationException($"File not found: {f}");
				var data = src.ReadAllBytes();
				File.WriteAllBytes(f, data);
				Console.WriteLine(f + " saved.");
			}
		}
	}
}
