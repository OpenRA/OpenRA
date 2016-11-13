#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

namespace OpenRA
{
	class GameMonitor
	{
		static readonly string[] VisualStudioRuntimeVersions = { "10.0", "11.0", "12.0" };

		static Process gameProcess;

		[STAThread]
		static void Main(string[] args)
		{
			if (Platform.CurrentPlatform == PlatformType.Windows)
			{
				var visualStudioRuntimeInstalled = 0;

				foreach (var version in VisualStudioRuntimeVersions)
				{
					var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DevDiv\VC\Servicing\{0}\RuntimeMinimum\".F(version), false);
					if (key != null && visualStudioRuntimeInstalled == 0)
						visualStudioRuntimeInstalled = (int)key.GetValue("Install", 0);

					key.Close();
				}

				if (visualStudioRuntimeInstalled == 0)
				{
					var result = MessageBox.Show("OpenRA needs the Microsoft Visual C++ 2010 (or above) x86 Redistributable installed.",
							"Visual C++ runtime not found.", MessageBoxButtons.OK, MessageBoxIcon.Error);

					if (result == DialogResult.OK)
						Process.Start("http://www.microsoft.com/en-us/download/details.aspx?id=5555");

					return;
				}
			}

			var executableDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var processName = Path.Combine(executableDirectory, "OpenRA.Game.exe");

			Directory.SetCurrentDirectory(executableDirectory);

			var psi = new ProcessStartInfo(processName, string.Join(" ", args.Select(arg => "\"" + arg + "\"")));

			try
			{
				gameProcess = Process.Start(psi);
			}
			catch
			{
				return;
			}

			if (gameProcess == null)
				return;

			gameProcess.EnableRaisingEvents = true;
			gameProcess.Exited += GameProcessExited;

			Application.Run();
		}

		static void ShowErrorDialog()
		{
			var form = new Form
			{
				Size = new Size(315, 140),
				Text = "Fatal Error",
				MinimizeBox = false,
				MaximizeBox = false,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterScreen,
				TopLevel = true,
				Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)
			};

			var notice = new Label
			{
				Location = new Point(10, 10),
				AutoSize = true,
				Text = "OpenRA has encountered a fatal error and must close.{0}Refer to the crash logs and FAQ for more information.".F(Environment.NewLine),
				TextAlign = ContentAlignment.TopCenter
			};

			var viewLogs = new Button
			{
				Location = new Point(10, 80),
				Size = new Size(75, 23),
				Text = "View Logs"
			};

			var viewFaq = new Button
			{
				Location = new Point(90, 80),
				Size = new Size(75, 23),
				Text = "View FAQ"
			};

			var quit = new Button
			{
				Location = new Point(225, 80),
				Size = new Size(75, 23),
				Text = "Quit",
				DialogResult = DialogResult.Cancel
			};

			form.Controls.Add(notice);
			form.Controls.Add(viewLogs);
			form.Controls.Add(viewFaq);
			form.Controls.Add(quit);

			viewLogs.Click += ViewLogsClicked;
			viewFaq.Click += ViewFaqClicked;
			form.FormClosed += FormClosed;

			SystemSounds.Exclamation.Play();
			form.ShowDialog();
		}

		static void GameProcessExited(object sender, EventArgs e)
		{
			if (!(gameProcess.ExitCode == (int)RunStatus.Success || gameProcess.ExitCode == (int)RunStatus.Restart))
				ShowErrorDialog();

			Exit();
		}

		static void ViewLogsClicked(object sender, EventArgs e)
		{
			try
			{
				Process.Start(Platform.ResolvePath("^", "Logs"));
			}
			catch
			{ }
		}

		static void ViewFaqClicked(object sender, EventArgs e)
		{
			try
			{
				Process.Start("http://wiki.openra.net/FAQ");
			}
			catch
			{ }
		}

		static void FormClosed(object sender, EventArgs e)
		{
			Exit();
		}

		static void Exit()
		{
			Environment.Exit(0);
		}
	}
}
