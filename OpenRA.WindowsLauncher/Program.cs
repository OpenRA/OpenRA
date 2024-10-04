#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
using System.Text;
using System.Threading.Tasks;
using Silk.NET.SDL;
using Thread = System.Threading.Thread;

namespace OpenRA.WindowsLauncher
{
	sealed class WindowsLauncher
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

			if (Array.Exists(args, x => x.StartsWith("Engine.LaunchPath=", StringComparison.Ordinal)))
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
			finally
			{
				// Flushing logs in finally block is okay here, as the catch block handles the exception.
				Log.Dispose();
			}
		}

		static int RunInnerLauncher(string[] args)
		{
			var launcherPath = Environment.ProcessPath;
			var launcherArgs = args.ToList();

			if (!launcherArgs.Exists(x => x.StartsWith("Engine.LaunchPath=", StringComparison.Ordinal)))
				launcherArgs.Add("Engine.LaunchPath=\"" + launcherPath + "\"");

			if (!launcherArgs.Exists(x => x.StartsWith("Game.Mod=", StringComparison.Ordinal)))
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

		static unsafe void ShowErrorDialog()
		{
			MessageBoxButtonData viewLogs;
			MessageBoxButtonData viewFaq;
			MessageBoxButtonData quit;

			var logsText = Encoding.ASCII.GetBytes("View Logs");
			fixed (byte* logsTextPtr = logsText)
			{
				viewLogs = new MessageBoxButtonData
				{
					Buttonid = 2,
					Text = logsTextPtr,
					Flags = (uint)MessageBoxButtonFlags.ReturnkeyDefault
				};
			}

			var faqText = Encoding.ASCII.GetBytes("View FAQ");
			fixed (byte* faqTextPtr = faqText)
			{
				viewFaq = new MessageBoxButtonData
				{
					Buttonid = 1,
					Text = faqTextPtr
				};
			}

			var quitText = Encoding.ASCII.GetBytes("Quit");
			fixed (byte* quitTextPtr = quitText)
			{
				quit = new MessageBoxButtonData
				{
					Buttonid = 0,
					Text = quitTextPtr,
					Flags = (uint)MessageBoxButtonFlags.EscapekeyDefault
				};
			}

			var buttons = new[] { quit, viewFaq, viewLogs };
			var title = Encoding.ASCII.GetBytes("Fatal Error");
			var message = Encoding.ASCII.GetBytes(displayName + " has encountered a fatal error and must close.\nRefer to the crash logs and FAQ for more information.");
			MessageBoxData dialog;

			fixed (MessageBoxButtonData* buttonsPtr = buttons)
			fixed (byte* titlePtr = title)
			fixed (byte* messagePtr = message)
			{
				dialog = new MessageBoxData
				{
					Flags = (uint)MessageBoxFlags.Error,
					Title = titlePtr,
					Message = messagePtr,
					Buttons = buttonsPtr,
					Numbuttons = 3
				};
			}

			// SDL_ShowMessageBox may create the error dialog behind other windows.
			// We want to bring it to the foreground, but can't do it from the main thread
			// because SDL_ShowMessageBox blocks until the user presses a button.
			// HACK: Spawn a thread to raise it to the foreground after a short delay.
			Task.Run(() =>
			{
				Thread.Sleep(1000);
				SetForegroundWindow(Process.GetCurrentProcess().MainWindowHandle);
			});

			var buttonid = 0;
			if (Sdl.GetApi().ShowMessageBox(in dialog, ref buttonid) < 0)
				Exit();

			switch (buttonid)
			{
				case 0: Exit(); break;
				case 1:
				{
					try
					{
						Sdl.GetApi().OpenURL(faqUrl);
					}
					catch { }
					break;
				}

				case 2:
				{
					try
					{
						Sdl.GetApi().OpenURL(Path.Combine(Platform.SupportDir, "Logs"));
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
