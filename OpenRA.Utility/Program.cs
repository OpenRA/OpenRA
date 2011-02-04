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
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace OpenRA.Utility
{
	class Program
	{
		delegate void ArgCallback(string[] args);

		static Dictionary<string, ArgCallback> argCallbacks;

		static void Main(string[] args)
		{
			argCallbacks = new Dictionary<string, ArgCallback>()
			{
				{ "--extract-zip-inner", Command.ExtractZip },
				{ "--install-ra-packages-inner", Command.InstallRAPackages },
				{ "--install-cnc-packages-inner", Command.InstallCncPackages },
				{ "--display-filepicker", Command.DisplayFilepicker },
				{ "--settings-value", Command.Settings },
				{ "--install-ra-packages", Command.AuthenticateAndInstallRAPackages },
				{ "--install-cnc-packages", Command.AuthenticateAndInstallCncPackages },
				{ "--extract-zip", Command.AuthenticateAndExtractZip },
			};

			if (args.Length == 0) { PrintUsage(); return; }

			bool piping = false;
			var i = Array.IndexOf(args, "--pipe");
			if (args.Length > 1 && i >= 0)
			{
				piping = true;
				string pipename = args[i+1];
				NamedPipeServerStream pipe;
				var id = WindowsIdentity.GetCurrent();
				var principal = new WindowsPrincipal(id);
				if (principal.IsInRole(WindowsBuiltInRole.Administrator))
				{
					var ps = new PipeSecurity();
					ps.AddAccessRule(new PipeAccessRule("EVERYONE", (PipeAccessRights)2032031, AccessControlType.Allow));
					pipe = new NamedPipeServerStream(pipename, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024*1024, 1024*1024, ps);
				}
				else
					pipe = new NamedPipeServerStream(pipename, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.None, 1024*1024,1024*1024,null);

				pipe.WaitForConnection();
				Console.SetOut(new StreamWriter(pipe) { AutoFlush = true });
			}

			ArgCallback callback;
			if (argCallbacks.TryGetValue(args[0], out callback))
				callback(args);
			else
				PrintUsage();

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
		}
	}
}
