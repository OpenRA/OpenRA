#region Copyright & License Information
/*
 * Copyright 2007-2010 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made 
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see LICENSE.
 */
#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.IO.Pipes;

namespace OpenRA.Utility
{
	class Program
	{
		static KeyValuePair<string, string> SplitArgs(string arg)
		{
			int i = arg.IndexOf('=');
			if (i < 0) return new KeyValuePair<string, string>(arg, "");
			return new KeyValuePair<string, string>(arg.Substring(0, i), arg.Substring(i + 1));
		}

		delegate void ArgCallback(string argValue);

		static Dictionary<string, ArgCallback> argCallbacks;

		static void Main(string[] args)
		{
			argCallbacks = new Dictionary<string, ArgCallback>();
			argCallbacks.Add("--list-mods", Command.ListMods);
			argCallbacks.Add("-l", Command.ListMods);
			argCallbacks.Add("--mod-info", Command.ListModInfo);
			argCallbacks.Add("-i", Command.ListModInfo);
			argCallbacks.Add("--download-url", Command.DownloadUrl);
			argCallbacks.Add("--extract-zip", Command.ExtractZip);
			argCallbacks.Add("--install-ra-packages", Command.InstallRAPackages);
			argCallbacks.Add("--install-cnc-packages", Command.InstallCncPackages);
			argCallbacks.Add("--settings-value", Command.Settings);

			if (args.Length == 0) { PrintUsage(); return; }
			var arg = SplitArgs(args[0]);

			bool piping = false;
			if (args.Length > 1 && args[1] == "--pipe")
			{
				piping = true;
				NamedPipeServerStream pipe;
				var id = WindowsIdentity.GetCurrent();
				var principal = new WindowsPrincipal(id);
				if (principal.IsInRole(WindowsBuiltInRole.Administrator))
				{
					var ps = new PipeSecurity();
					ps.AddAccessRule(new PipeAccessRule("EVERYONE", PipeAccessRights.FullControl, System.Security.AccessControl.AccessControlType.Allow));
					pipe = new NamedPipeServerStream("OpenRA.Utility", PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024, 1024, ps);
				}
				else
					pipe = new NamedPipeServerStream("OpenRA.Utility", PipeDirection.Out);

				pipe.WaitForConnection();
				Console.SetOut(new StreamWriter(pipe) { AutoFlush = true });
			}

			ArgCallback callback;
			if (argCallbacks.TryGetValue(arg.Key, out callback))
				callback(arg.Value);
			else
				PrintUsage();

			if (piping)
				Console.Out.Close();
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: OpenRA.Utility.exe [OPTION]");
			Console.WriteLine();
			Console.WriteLine("  -l,--list-mods                   List currently installed mods");
			Console.WriteLine("  -i=MODS,--mod-info=MODS          List metadata for MODS (comma separated list of mods)");
			Console.WriteLine("  --download-url=URL,DEST          Download a file from URL to DEST");
			Console.WriteLine("  --extract-zip=ZIPFILE,MOD,PATH   Extract the zip ZIPFILE to DEST relative to MOD");
			Console.WriteLine("  --install-ra-packages=PATH       Install required packages for RA from CD to PATH");
			Console.WriteLine("  --install-cnc-packages=PATH      Install required packages for C&C from CD to PATH");
			Console.WriteLine("  --settings-value=SUPPORTDIR,KEY  Get value of KEY in SUPPORTDIR/settings.yaml");
		}
	}
}
