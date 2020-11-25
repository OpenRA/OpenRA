#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SDL2;

namespace OpenRA.WindowsLauncher
{
	class WindowsLauncher
	{
		[DllImport("user32.dll")]
		static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32.dll")]
		static extern bool AllowSetForegroundWindow(int dwProcessId);

		static Process gameProcess;
		static string modID;
		static string displayName;
		static string faqUrl;

		static int Main(string[] args)
		{
			// The modID, displayName, and faqUrl variables are embedded in the assembly metadata by defining
			// -p:ModID="mymod", -p:DisplayName="My Mod", -p:FaqUrl="https://my.tld/faq" when compiling the project
			var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes();
			foreach (var a in attributes)
			{
				if (a is AssemblyMetadataAttribute metadata)
				{
					switch (metadata.Key)
					{
						case "ModID": modID = metadata.Value; break;
						case "DisplayName": displayName = metadata.Value; break;
						case "FaqUrl": faqUrl = metadata.Value; break;
					}
				}
			}

			if (args.Any(x => x.StartsWith("Engine.LaunchPath=", StringComparison.Ordinal)))
				return RunGame(args);

			return RunInnerLauncher(args);
		}

		static int RunGame(string[] args)
		{
			var launcherPath = Assembly.GetExecutingAssembly().Location;
			var directory = Path.GetDirectoryName(launcherPath);
			Directory.SetCurrentDirectory(directory);

			AppDomain.CurrentDomain.UnhandledException += (_, e) => ExceptionHandler.HandleFatalError((Exception)e.ExceptionObject);

			try
			{
				return (int)Game.InitializeAndRun(args);
			}
			catch (Exception e)
			{
				// We must grant permission for the launcher process to bring the error dialog to the foreground.
				// Finding the parent process id is unreasonably difficult on Windows, so instead pass -1 to enable for all processes.
				AllowSetForegroundWindow(-1);
				ExceptionHandler.HandleFatalError(e);
				return (int)RunStatus.Error;
			}
		}

		static int RunInnerLauncher(string[] args)
		{
			var launcherPath = Process.GetCurrentProcess().MainModule.FileName;
			var launcherArgs = args.ToList();

			if (!launcherArgs.Any(x => x.StartsWith("Engine.LaunchPath=", StringComparison.Ordinal)))
				launcherArgs.Add("Engine.LaunchPath=\"" + launcherPath + "\"");

			if (!launcherArgs.Any(x => x.StartsWith("Game.Mod=", StringComparison.Ordinal)))
				launcherArgs.Add("Game.Mod=" + modID);

			var psi = new ProcessStartInfo(launcherPath, string.Join(" ", launcherArgs));

			try
			{
				gameProcess = Process.Start(psi);
			}
			catch
			{
				return 1;
			}

			if (gameProcess == null)
				return 1;

			gameProcess.EnableRaisingEvents = true;
			gameProcess.Exited += GameProcessExited;
			gameProcess.WaitForExit();

			return 0;
		}

		static void ShowErrorDialog()
		{
			var viewLogs = new SDL.SDL_MessageBoxButtonData
			{
				buttonid = 2,
				text = "View Logs",
				flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT
			};

			var viewFaq = new SDL.SDL_MessageBoxButtonData
			{
				buttonid = 1,
				text = "View FAQ"
			};

			var quit = new SDL.SDL_MessageBoxButtonData
			{
				buttonid = 0,
				text = "Quit",
				flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_ESCAPEKEY_DEFAULT
			};

			var dialog = new SDL.SDL_MessageBoxData
			{
				flags = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
				title = "Fatal Error",
				message = displayName + " has encountered a fatal error and must close.\nRefer to the crash logs and FAQ for more information.",
				buttons = new[] { quit, viewFaq, viewLogs },
				numbuttons = 3
			};

			// SDL_ShowMessageBox may create the error dialog behind other windows.
			// We want to bring it to the foreground, but can't do it from the main thread
			// because SDL_ShowMessageBox blocks until the user presses a button.
			// HACK: Spawn a thread to raise it to the foreground after a short delay.
			Task.Run(() =>
			{
				Thread.Sleep(1000);
				SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
			});

			if (SDL.SDL_ShowMessageBox(ref dialog, out var buttonid) < 0)
				Exit();

			switch (buttonid)
			{
				case 0: Exit(); break;
				case 1:
				{
					try
					{
						Process.Start(faqUrl);
					}
					catch { }
					break;
				}

				case 2:
				{
					try
					{
						Process.Start(Path.Combine(Platform.SupportDir, "Logs"));
					}
					catch { }
					break;
				}
			}
		}

		static void GameProcessExited(object sender, EventArgs e)
		{
			if (gameProcess.ExitCode != (int)RunStatus.Success)
				ShowErrorDialog();

			Exit();
		}

		static void Exit()
		{
			Environment.Exit(0);
		}
	}
}
