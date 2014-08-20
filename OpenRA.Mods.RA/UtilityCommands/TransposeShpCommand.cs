#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using OpenRA.FileFormats;

namespace OpenRA.Mods.RA.UtilityCommands
{
	class TransposeShpCommand : IUtilityCommand
	{
		public string Name { get { return "--transpose"; } }

		[Desc("SRCSHP DESTSHP START N M [START N M ...]",
			  "Transpose the N*M block of frames starting at START.")]
		public void Run(string[] args)
		{
			var srcImage = ShpReader.Load(args[1]);

			var srcFrames = srcImage.Frames;
			var destFrames = srcImage.Frames.ToArray();

			for (var z = 3; z < args.Length - 2; z += 3)
			{
				var start = Exts.ParseIntegerInvariant(args[z]);
				var m = Exts.ParseIntegerInvariant(args[z + 1]);
				var n = Exts.ParseIntegerInvariant(args[z + 2]);

				for (var i = 0; i < m; i++)
					for (var j = 0; j < n; j++)
						destFrames[start + i * n + j] = srcFrames[start + j * m + i];
			}

			using (var destStream = File.Create(args[2]))
				ShpReader.Write(destStream, srcImage.Size, destFrames.Select(f => f.Data));
		}
	}
}
