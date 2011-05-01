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
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace OpenRA.Utility
{
	class Program
	{
		const int PipeBufferSize = 1024 * 1024;

		static PipeSecurity MakePipeSecurity()
		{
			var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
			if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
				return null;	// no special pipe security required

			var ps = new PipeSecurity();
			ps.AddAccessRule(new PipeAccessRule("EVERYONE", (PipeAccessRights)2032031, AccessControlType.Allow));
			return ps;	
		}
		
		static void Main(string[] args)
		{
			var actions = new Dictionary<string, Action<string[]>>()
			{
				{ "--extract-zip-inner", Command.ExtractZip },
				{ "--install-ra-packages-inner", Command.InstallRAPackages },
				{ "--install-cnc-packages-inner", Command.InstallCncPackages },
				{ "--display-filepicker", Command.DisplayFilepicker },
				{ "--settings-value", Command.Settings },
				{ "--install-ra-packages", Command.AuthenticateAndInstallRAPackages },
				{ "--install-cnc-packages", Command.AuthenticateAndInstallCncPackages },
				{ "--extract-zip", Command.AuthenticateAndExtractZip },
				{ "--shp", Command.ConvertPngToShp },
				{ "--png", Command.ConvertShpToPng },
			};

			if (args.Length == 0) { PrintUsage(); return; }

            var pipename = GetNamedArg(args, "--pipe");
            var piping = pipename != null;
            if (pipename != null)
            {
                var pipe = new NamedPipeServerStream(pipename, PipeDirection.Out, 1,
                    PipeTransmissionMode.Byte, PipeOptions.None, PipeBufferSize, PipeBufferSize,
                    MakePipeSecurity());

                pipe.WaitForConnection();
                Console.SetOut(new StreamWriter(pipe) { AutoFlush = true });
            }

            var supportDir = GetNamedArg(args, "--SupportDir");
            if (supportDir != null)
            {
                Log.LogPath = Path.Combine(supportDir.Replace("\"", ""), "Logs");
                Console.WriteLine("LogPath: {0}", Log.LogPath);
            }

			try
			{
				var action = WithDefault( null, () => actions[args[0]]);
				if (action == null)
					PrintUsage();
				else
					action(args);
			}
			catch( Exception e )
			{
				Log.AddChannel("utility", "utility.log");
				Log.Write("utility", "Received args: {0}", string.Join(" ", args));
				Log.Write("utility", "{0}", e.ToString());
				
				Console.WriteLine("Error: Utility application crashed. See utility.log for details");
				if (piping)
					Console.Out.Close();
				throw;
			}

			if (piping)
				Console.Out.Close();
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: OpenRA.Utility.exe [OPTION] [ARGS]");
			Console.WriteLine();
			Console.WriteLine("  --extract-zip ZIPFILE PATH       Extract the zip ZIPFILE to DEST (relative to openra dir)");
			Console.WriteLine("  --install-ra-packages PATH       Install required packages for RA from CD to PATH");
			Console.WriteLine("  --install-cnc-packages PATH      Install required packages for C&C from CD to PATH");
			Console.WriteLine("  --settings-value SUPPORTDIR KEY  Get value of KEY in SUPPORTDIR/settings.yaml");
			Console.WriteLine("  --shp PNGFILE FRAMEWIDTH         Convert a PNG containing one or more frames to a SHP");
			Console.WriteLine("  --png SHPFILE                    Convert a SHP to a PNG containing all of its frames");
		}

		static T WithDefault<T>(T def, Func<T> f)
		{
			try { return f(); }
			catch { return def; }
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
