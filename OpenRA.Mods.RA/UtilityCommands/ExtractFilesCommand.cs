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

namespace OpenRA.Mods.RA.UtilityCommands
{
	class ExtractFilesCommand : IUtilityCommand
	{
		public string Name { get { return "--extract"; } }

		[Desc("MOD FILES", "Extract files from mod packages to the current directory")]
		public void Run(string[] args)
		{
			var mod = args[1];
			var files = args.Skip(2);

			var manifest = new Manifest(mod);
			GlobalFileSystem.LoadFromManifest(manifest);

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
