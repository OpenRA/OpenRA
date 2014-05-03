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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using OpenRA.FileFormats;

namespace OpenRA
{
	public enum PlatformType { Unknown, Windows, OSX, Linux }

	public static class Platform
	{
		public static PlatformType CurrentPlatform { get { return currentPlatform.Value; } }

		static Lazy<PlatformType> currentPlatform = new Lazy<PlatformType>(GetCurrentPlatform);

		static PlatformType GetCurrentPlatform()
		{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
					return PlatformType.Windows;

				try
				{
					var psi = new ProcessStartInfo("uname", "-s");
					psi.UseShellExecute = false;
					psi.RedirectStandardOutput = true;
					var p = Process.Start(psi);
					var kernelName = p.StandardOutput.ReadToEnd();
					if (kernelName.Contains("Linux") || kernelName.Contains("BSD"))
						return PlatformType.Linux;
					if (kernelName.Contains("Darwin"))
						return PlatformType.OSX;
				}
				catch {	}

				return PlatformType.Unknown;
		}

		public static string RuntimeVersion
		{
			get
			{
				var mono = Type.GetType("Mono.Runtime");
				if (mono == null)
					return ".NET CLR {0}".F(Environment.Version);

				var version = mono.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
				if (version == null)
					return "Mono (unknown version) CLR {0}".F(Environment.Version);

				return "Mono {0} CLR {1}".F(version.Invoke(null, null), Environment.Version); 
			}
		}

		public static string SupportDir
		{
			get
			{
				// Use a local directory in the game root if it exists
				if (Directory.Exists("Support"))
					return "Support" + Path.DirectorySeparatorChar;

				var dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

				switch (CurrentPlatform)
				{
				case PlatformType.Windows:
					dir += Path.DirectorySeparatorChar + "OpenRA";
					break;
				case PlatformType.OSX:
					dir += "/Library/Application Support/OpenRA";
					break;
				case PlatformType.Linux:
				default:
					dir += "/.openra";
					break;
				}

				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				return dir + Path.DirectorySeparatorChar;
			}
		}

		public static void ShowFatalErrorDialog()
		{
			var process = "OpenRA.CrashDialog.exe";
			var args = "";

			if (CurrentPlatform == PlatformType.OSX)
			{
				// Winforms requires X11 under OSX, which may not exist.
				// Show a native dialog using applescript instead.
				var title = "Fatal Error";
				var logsButton = "View Logs";
				var faqButton = "View FAQ";
				var quitButton = "Quit";
				var message = "OpenRA has encountered a fatal error.\nRefer to the crash logs and FAQ for more information.";

				var faqPath = Game.Settings.Debug.FatalErrorDialogFaq;
				var logsPath = "file://" + Platform.SupportDir + "Logs" + Path.DirectorySeparatorChar;

				var iconPath = new [] { "./OpenRA.icns", "./packaging/osx/template.app/Contents/Resources/OpenRA.icns" }.FirstOrDefault(f => File.Exists(f));
				var icon = iconPath != null ? "with icon alias (POSIX file \\\"file://{0}\\\")".F(Environment.CurrentDirectory + "/" + iconPath) : "";

				process = "/usr/bin/osascript";
				args = (
					"-e \"repeat\"\n" +
					"-e \"  tell application \\\"Finder\\\"\"\n" +
					"-e \"    set question to display dialog \\\"{1}\\\" with title \\\"{0}\\\" {2} buttons {{\\\"{3}\\\", \\\"{5}\\\", \\\"{7}\\\"}} default button 3\"\n" +
					"-e \"    if button returned of question is equal to \\\"{3}\\\" then open (POSIX file \\\"{4}\\\")\"\n" +
					"-e \"    if button returned of question is equal to \\\"{5}\\\" then open location \\\"{6}\\\"\"\n" +
					"-e \"    if button returned of question is equal to \\\"{7}\\\" then exit repeat\"\n" +
					"-e \"    activate\"\n" +
					"-e \"  end tell\"\n" +
					"-e \"end repeat\""
				).F(title, message, icon, logsButton, logsPath, faqButton, faqPath, quitButton);
			}

			var psi = new ProcessStartInfo(process, args);
			psi.UseShellExecute = false;
			psi.CreateNoWindow = true;
			Process.Start(psi);
		}
	}
}
