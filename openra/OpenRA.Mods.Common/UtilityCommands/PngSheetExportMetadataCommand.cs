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

using System.IO;
using System.Linq;
using OpenRA.FileFormats;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class PngSheetExportMetadataCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--png-sheet-export";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 2;
		}

		[Desc("PNGFILE", "Export png metadata to yaml")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			using (var s = File.OpenRead(args[1]))
			{
				var png = new Png(s);
				png.EmbeddedData.Select(m => new MiniYamlNode(m.Key, m.Value))
					.ToList()
					.WriteToFile(Path.ChangeExtension(args[1], "yaml"));
			}
		}
	}
}
