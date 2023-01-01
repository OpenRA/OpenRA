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
using OpenRA.FileFormats;
using OpenRA.Graphics;
using OpenRA.Mods.Cnc.SpriteLoaders;
using OpenRA.Primitives;

namespace OpenRA.Mods.Cnc.UtilityCommands
{
	class ConvertPngToShpCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--shp";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("PNGFILE [PNGFILE ...]", "Combine a list of PNG images into a SHP")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var inputFiles = GlobArgs(args).OrderBy(a => a).ToList();
			var dest = inputFiles[0].Split('-').First() + ".shp";

			var frames = inputFiles.Select(a => new Png(File.OpenRead(a))).ToList();
			if (frames.Any(f => f.Type != SpriteFrameType.Indexed8))
				throw new InvalidOperationException("All frames must be paletted");

			var size = new Size(frames[0].Width, frames[0].Height);
			if (frames.Any(f => f.Width != size.Width || f.Height != size.Height))
				throw new InvalidOperationException("All frames must be the same size");

			using (var destStream = File.Create(dest))
				ShpTDSprite.Write(destStream, size, frames.Select(f => f.Data));

			Console.WriteLine(dest + " saved.");
		}

		static IEnumerable<string> GlobArgs(string[] args, int startIndex = 1)
		{
			for (var i = startIndex; i < args.Length; i++)
				foreach (var path in Glob.Expand(args[i]))
					yield return path;
		}
	}
}
