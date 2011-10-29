#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;

namespace OpenRA.Utility
{
	class Program
	{
		static void Main(string[] args)
		{
			var actions = new Dictionary<string, Action<string[]>>()
			{
				{ "--settings-value", Command.Settings },
				{ "--shp", Command.ConvertPngToShp },
				{ "--png", Command.ConvertShpToPng },
				{ "--fromd2", Command.ConvertFormat2ToFormat80 },
				{ "--extract", Command.ExtractFiles },
				{ "--tmp-png", Command.ConvertTmpToPng },
			};

			if (args.Length == 0) { PrintUsage(); return; }

			Log.LogPath = Platform.SupportDir + "Logs" + Path.DirectorySeparatorChar;

			try
			{
				var action = Exts.WithDefault( _ => PrintUsage(), () => actions[args[0]]);
				action(args);
			}
			catch( Exception e )
			{
				Log.AddChannel("utility", "utility.log");
				Log.Write("utility", "Received args: {0}", args.JoinWith(" "));
				Log.Write("utility", "{0}", e);

				Console.WriteLine("Error: Utility application crashed. See utility.log for details");
				throw;
			}
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: OpenRA.Utility.exe [OPTION] [ARGS]");
			Console.WriteLine();
			Console.WriteLine("  --settings-value KEY             Get value of KEY from settings.yaml");
			Console.WriteLine("  --shp PNGFILE FRAMEWIDTH         Convert a PNG containing one or more frames to a SHP");
			Console.WriteLine("  --png SHPFILE PALETTE [--transparent] Convert a SHP to a PNG containing all of its frames, optionally setting up transparency");
			Console.WriteLine("  --extract MOD[,MOD]* FILES		  Extract files from mod packages");
			Console.WriteLine("  --tmp-png MOD[,MOD]* THEATER FILES        Extract terrain tiles to PNG");
		}

		static string GetNamedArg(string[] args, string arg)
		{
			if (args.Length < 2)
				return null;

			var i = Array.IndexOf(args, arg);
			if (i < 0 || i == args.Length - 1)  // doesnt exist, or doesnt have a value.
				return null;

			return args[i + 1];
		}
	}
}
