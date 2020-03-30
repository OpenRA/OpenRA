#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.IO;
using OpenRA.FileFormats;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class PngSheetImportMetadataCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--png-sheet-import"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length == 2;
		}

		[Desc("PNGFILE", "Import yaml metadata to png")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Png png;
			using (var pngStream = File.OpenRead(args[1]))
				png = new Png(pngStream);

			foreach (var node in MiniYaml.FromFile(Path.ChangeExtension(args[1], "yaml")))
				png.EmbeddedData[node.Key] = node.Value.Value;

			png.Save(args[1]);
		}
	}
}
