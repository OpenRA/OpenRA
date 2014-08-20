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
using OpenRA.FileSystem;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class ExtractFilesCommand : IUtilityCommand
	{
		public string Name { get { return "--extract"; } }

		[Desc("Extract files from mod packages to the current directory")]
		public void Run(ModData modData, string[] args)
		{
			var files = args.Skip(1);
			GlobalFileSystem.LoadFromManifest(modData.Manifest);

			foreach (var f in files)
			{
				var src = GlobalFileSystem.Open(f);
				if (src == null)
					throw new InvalidOperationException("File not found: {0}".F(f));
				var data = src.ReadAllBytes();
				File.WriteAllBytes(f, data);
				Console.WriteLine(f + " saved.");
			}
		}
	}
}
