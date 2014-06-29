#region Copyright & License Information
/*
 * Copyright 2007-2014 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Forms;

namespace OpenRA.CrashDialog
{
	class FatalErrorDialog
	{
		static Settings settings;
		[STAThread]
		public static void Main(string[] args)
		{
			settings = new Settings(Platform.SupportDir + "settings.yaml", new Arguments());

			var form = new Form
			{
				Size = new Size(315, 140),
				Text = "Fatal Error",
				MinimizeBox = false,
				MaximizeBox = false,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterScreen,
				Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location)
			};

			var notice = new Label
			{
				Location = new Point(10, 10),
				AutoSize = true,
				Text = "OpenRA has encountered a fatal error and must close.{0}Refer to the crash logs and FAQ for more information.".F(Environment.NewLine),
				TextAlign = ContentAlignment.TopCenter
			};
			form.Controls.Add(notice);

			var dontShowAgain = new CheckBox
			{
				Location = new Point(25, 50),
				AutoSize = true,
				Text = "Don't show this message again",
			};
			form.Controls.Add(dontShowAgain);

			var viewLogs = new Button
			{
				Location = new Point(10, 80),
				Size = new Size(75, 23),
				Text = "View Logs"
			};
			viewLogs.Click += ViewLogsClicked;
			form.Controls.Add(viewLogs);

			var viewFaq = new Button
			{
				Location = new Point(90, 80),
				Size = new Size(75, 23),
				Text = "View FAQ"
			};
			viewFaq.Click += ViewFaqClicked;
			form.Controls.Add(viewFaq);

			var quit = new Button
			{
				Location = new Point(225, 80),
				Size = new Size(75, 23),
				Text = "Quit"
			};
			quit.DialogResult = DialogResult.Cancel;
			form.Controls.Add(quit);

			form.FormClosed += (sender, e) =>
			{
				settings.Debug.ShowFatalErrorDialog = !dontShowAgain.Checked;
				settings.Save();
			};

			SystemSounds.Exclamation.Play();
			form.ShowDialog();
		}

		static void ViewLogsClicked(object sender, EventArgs e)
		{
			try
			{
				Game.Settings = settings;
				Process.Start(Platform.GetFolderPath(UserFolder.Logs));
			}
			catch { }
		}

		static void ViewFaqClicked(object sender, EventArgs e)
		{
			try
			{
				Process.Start(settings.Debug.FatalErrorDialogFaq);
			}
			catch { }
		}
	}
}
