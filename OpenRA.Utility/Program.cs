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
using System.Linq;
using System.Text;
using OpenRA.FileFormats;
using System.IO;
using System.Net;
using System.Threading;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

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
			argCallbacks.Add("--list-mods", ListMods);
			argCallbacks.Add("--mod-info", ListModInfo);
			argCallbacks.Add("--install-ra-music", InstallRAMusic);
			argCallbacks.Add("--install-cnc-music", InstallCncMusic);
			argCallbacks.Add("--download-packages", DownloadPackage);
			
			if (args.Length == 0) { PrintUsage(); return; }
			var arg = SplitArgs(args[0]);
			ArgCallback callback;
			if (argCallbacks.TryGetValue(arg.Key, out callback))
				callback(arg.Value);
			else
				PrintUsage();
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage: OpenRA.Utility.exe [OPTION]");
			Console.WriteLine();
			Console.WriteLine("  --list-mods                      List currently installed mods");
			Console.WriteLine("  --mod-info=MODS                  List metadata for MODS (comma separated list of mods)");
			Console.WriteLine("  --install-ra-music=PATH          Install scores.mix from PATH to Red Alert CD");
			Console.WriteLine("  --install-cnc-music=PATH         Install scores.mix from PATH to Command & Conquer CD");
			Console.WriteLine("  --download-packages=MOD{,PATH}   Download packages for MOD to PATH (def: system temp folder) and install them");
		}

		static void ListMods(string _)
		{
			foreach (var m in Mod.AllMods)
				Console.WriteLine(m.Key);
		}

		static void ListModInfo(string modList)
		{
			string[] mods = modList.Split(',');
			foreach (var m in mods)
			{
				var mod = Mod.AllMods
					.Where(x => x.Key.Equals(m))
					.Select(x => x.Value)
					.FirstOrDefault();
				if (mod == null)
				{
					Console.WriteLine("Error: Mod `{0}` is not installed or could not be found.", m);
					return;
				}

				Console.WriteLine("{0}:", m);
				Console.WriteLine("  Title: {0}", mod.Title);
				Console.WriteLine("  Version: {0}", mod.Version);
				Console.WriteLine("  Author: {0}", mod.Author);
				Console.WriteLine("  Description: {0}", mod.Description);
				Console.WriteLine("  Requires: {0}", mod.RequiresMods == null ? "" : string.Join(",", mod.RequiresMods));
				Console.WriteLine("  Standalone: {0}", mod.Standalone.ToString());
			}
		}

		static void InstallRAMusic(string path)
		{
			if (!Directory.Exists(path)) { Console.WriteLine("Error: Path {0} does not exist", path); return; }
			FileSystem.Mount(path);
			if (!FileSystem.Exists("MAIN.MIX")) { Console.WriteLine("Error: Could not find MAIN.MIX in path {0}", path); return; }
			FileSystem.Mount("MAIN.MIX");

			using (var scoresStream = FileSystem.Open("scores.mix"))
				using (var destStream = File.Create(string.Format("mods{0}ra{0}packages{0}scores.mix", Path.DirectorySeparatorChar)))
					destStream.Write(scoresStream.ReadAllBytes());

			Console.WriteLine("Done");
		}

		static void InstallCncMusic(string path)
		{
			if (!Directory.Exists(path)) { Console.WriteLine("Error: Path {0} does not exist", path); return; }
			string scoresMixPath = path + Path.DirectorySeparatorChar + "SCORES.MIX";
			if (!File.Exists(scoresMixPath)) { Console.WriteLine("Error: Could not find SCORES.MIX in path {0}", path); return; }

			File.Copy(scoresMixPath, string.Format("mods{0}cnc{0}packages{0}scores.mix", Path.DirectorySeparatorChar), true);

			Console.WriteLine("Done");
		}

		static void DownloadPackage(string argValue)
		{
			string[] args = argValue.Split(',');
			string mod = "";
			string destPath = Path.GetTempPath();
			
			if (args.Length >= 1) 
				mod = args[0];
			if (args.Length >= 2)
				destPath = args[1];
			
			string destFile = string.Format("{0}{1}{2}-packages.zip", destPath, Path.DirectorySeparatorChar, mod);
			
			if (File.Exists(destFile))
			{
				Console.WriteLine ("Downloaded file already exists, using it instead.");
				DownloadFileCompleted(null, 
					new System.ComponentModel.AsyncCompletedEventArgs(null, false, new string[] { mod, destPath }));
				return;
			}
			
			WebClient wc = new WebClient();
			wc.DownloadProgressChanged += DownloadProgressChanged;
			wc.DownloadFileCompleted += DownloadFileCompleted;
			Console.WriteLine("Downloading {0}-packages.zip to {1}", mod, destPath);
			wc.DownloadFileAsync(
				new Uri(string.Format("http://open-ra.org/get-dependency.php?file={0}-packages", mod)),
				destFile, 
				new string[] { mod, destPath });

			while (wc.IsBusy)
				Thread.Sleep(500);
		}

		static void DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				Console.WriteLine("Error: {0}", e.Error.Message);
				return;
			}

			Console.WriteLine("Download Completed");
			string[] modAndDest = (string[])e.UserState;
			string mod = modAndDest[0];
			string dest = modAndDest[1];
			string filepath = string.Format("{0}{1}{2}-packages.zip", dest, Path.DirectorySeparatorChar, mod);
			string modPackageDir = string.Format("mods{0}{1}{0}packages{0}", Path.DirectorySeparatorChar, mod);
			
			using (var z = new ZipInputStream(File.OpenRead(filepath)))
			{
				ZipEntry entry;
				while ((entry = z.GetNextEntry()) != null)
				{
					if (!entry.IsFile) continue;
					
					Console.WriteLine ("Extracting {0}", entry.Name);
					using (var f = File.Create(modPackageDir + entry.Name))
					{
						int bufSize = 2048;
						byte[] buf = new byte[bufSize];
						while ((bufSize = z.Read(buf, 0, buf.Length)) > 0)
						{
							f.Write(buf, 0, bufSize);
						}
					}
				}
			}
			
			Console.WriteLine ("Done");
		}

		static void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
		{
			Console.WriteLine("{0}% {1}/{2} bytes", e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
		}
	}
}
