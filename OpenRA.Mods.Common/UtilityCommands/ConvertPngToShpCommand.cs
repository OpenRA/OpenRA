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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using OpenRA.FileFormats;
using OpenRA.Mods.Common.SpriteLoaders;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ConvertPngToShpCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--shp"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("PNGFILE [PNGFILE ...]", "Combine a list of PNG images into a SHP")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var inputFiles = GlobArgs(args).OrderBy(a => a).ToList();
			var dest = inputFiles[0].Split('-').First() + ".shp";
			var frames = inputFiles.Select(a => PngLoader.Load(a));

			var size = frames.First().Size;
			if (frames.Any(f => f.Size != size))
				throw new InvalidOperationException("All frames must be the same size");

			using (var destStream = File.Create(dest))
				ShpTDSprite.Write(destStream, size, frames.Select(f => ToBytes(f)));

			Console.WriteLine(dest + " saved.");
		}

		static IEnumerable<string> GlobArgs(string[] args, int startIndex = 1)
		{
			for (var i = startIndex; i < args.Length; i++)
				foreach (var path in Glob.Expand(args[i]))
					yield return path;
		}

		static byte[] ToBytes(Bitmap bitmap)
		{
			var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
				PixelFormat.Format8bppIndexed);

			var bytes = new byte[bitmap.Width * bitmap.Height];
			for (var i = 0; i < bitmap.Height; i++)
				Marshal.Copy(new IntPtr(data.Scan0.ToInt64() + i * data.Stride),
					bytes, i * bitmap.Width, bitmap.Width);

			bitmap.UnlockBits(data);

			return bytes;
		}
	}
}
