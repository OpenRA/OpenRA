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
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using OpenRA.FileFormats;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Pipes;

namespace OpenRA.Utility
{
	static class Util
	{
		public static void ExtractFromPackage(string srcPath, string package, string[] files, string destPath)
		{
			if (!Directory.Exists(srcPath)) { Console.WriteLine("Error: Path {0} does not exist", srcPath); return; }
			if (!Directory.Exists(destPath)) { Console.WriteLine("Error: Path {0} does not exist", destPath); return; }

			FileSystem.Mount(srcPath);
			if (!FileSystem.Exists(package)) { Console.WriteLine("Error: Could not find {0}", package); return; }
			FileSystem.Mount(package);

			foreach (string s in files)
			{
				var destFile = "{0}{1}{2}".F(destPath, Path.DirectorySeparatorChar, s);
				using (var sourceStream = FileSystem.Open(s))
				using (var destStream = File.Create(destFile))
				{
					Console.WriteLine("Status: Extracting {0}", s);
					destStream.Write(sourceStream.ReadAllBytes());
				}
			}
		}

		public static void ExtractZip(this ZipInputStream z, string destPath, List<string> extracted)
		{
			ZipEntry entry;
			while ((entry = z.GetNextEntry()) != null)
			{
				if (!entry.IsFile) continue;

				Console.WriteLine("Status: Extracting {0}", entry.Name);
				if (!Directory.Exists(Path.Combine(destPath, Path.GetDirectoryName(entry.Name))))
					Directory.CreateDirectory(Path.Combine(destPath, Path.GetDirectoryName(entry.Name)));
				var path = destPath + Path.DirectorySeparatorChar + entry.Name;
				extracted.Add(path);
				using (var f = File.Create(path))
				{
					int bufSize = 2048;
					byte[] buf = new byte[bufSize];
					while ((bufSize = z.Read(buf, 0, buf.Length)) > 0)
						f.Write(buf, 0, bufSize);
				}
			}
			z.Close();
		}
		
		public static string GetPipeName()
		{
			return "OpenRA.Utility" + Guid.NewGuid().ToString();
		}	
		
		public static void CallWithAdmin(string command)
		{
			switch (Environment.OSVersion.Platform)
			{
			case PlatformID.Unix:
				if (File.Exists("/usr/bin/gksudo"))
					CallWithAdminGnome(command);
				else if (File.Exists("/usr/bin/kdesudo"))
					CallWithAdminKDE(command);
				else
					CallWithoutAdmin(command);
				break;
			default:
				CallWithAdminWindows(command);
				break;
			}
		}
		
		static void CallWithAdminGnome(string command)
		{
			var p = new Process();
			p.StartInfo.FileName = "/usr/bin/gksudo";
			p.StartInfo.Arguments = "--description \"OpenRA Utility App\" -- mono OpenRA.Utility.exe " + command;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.Start();
			
			using (var reader = p.StandardOutput)
			{
				while (!p.HasExited)
				{
					string line = reader.ReadLine();
					if (string.IsNullOrEmpty(line)) continue;
					if (line.Equals("GNOME_SUDO_PASSGNOME_SUDO_PASSSorry, try again.")) //gksudo is slightly moronic
					{
						Console.WriteLine("Error: Could not elevate process");
						return;
					}
					else
						Console.WriteLine(line);
				}
			}
		}
		
		static void CallWithAdminKDE(string command)
		{
			var p = new Process();
			p.StartInfo.FileName = "/usr/bin/kdesudo";
			p.StartInfo.Arguments = "-d -- mono OpenRA.Utility.exe " + command;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.Start();
			
			using (var reader = p.StandardOutput)
			{
				while(!p.HasExited)
				{
					Console.WriteLine(reader.ReadLine());
				}
			}
		}
		
		static void CallWithAdminWindows(string command)
		{			
			string pipename = Util.GetPipeName();
			var p = new Process();
			p.StartInfo.FileName = "OpenRA.Utility.exe";
			p.StartInfo.Arguments = command + " --pipe " + pipename;
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.Verb = "runas";

			try
			{
				p.Start();
			}
			catch (Win32Exception e)
			{
				if (e.NativeErrorCode == 1223) //ERROR_CANCELLED
					return;
				throw e;
			}
			
			var pipe = new NamedPipeClientStream(".", pipename, PipeDirection.In);
			pipe.Connect();

			using (var reader = new StreamReader(pipe))
			{
				while (!p.HasExited)
					Console.WriteLine(reader.ReadLine());
			}
		}
		
		static void CallWithoutAdmin(string command)
		{
			var p = new Process();
			p.StartInfo.FileName = "mono";
			p.StartInfo.Arguments = "OpenRA.Utility.exe " + command;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.Start();
			
			using (var reader = p.StandardOutput)
			{
				while(!p.HasExited)
					Console.WriteLine(reader.ReadLine());
			}
		}
	}
}
