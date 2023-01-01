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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UtilityCommands
{
	public class PngSheetImportMetadataCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--png-sheet-import";

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

			var yaml = MiniYaml.FromFile(Path.ChangeExtension(args[1], "yaml"));

			var frameSizeField = yaml.Where(y => y.Key == "FrameSize").Select(y => y.Value.Value).FirstOrDefault();
			if (frameSizeField != null)
			{
				var frameSize = FieldLoader.GetValue<Size>("FrameSize", frameSizeField);

				var frameAmountField = yaml.Where(y => y.Key == "FrameAmount").Select(y => y.Value.Value).FirstOrDefault();
				if (frameAmountField != null)
				{
					var frameAmount = FieldLoader.GetValue<int>("FrameAmount", frameAmountField);
					if (frameAmount > (png.Width / frameSize.Width) * (png.Height / frameSize.Height))
						throw new InvalidDataException(".png file is too small for given FrameSize and FrameAmount.");
				}
			}

			foreach (var node in yaml)
				png.EmbeddedData[node.Key] = node.Value.Value;

			png.Save(args[1]);
		}
	}
}
