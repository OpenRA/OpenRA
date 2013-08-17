#region Copyright & License Information
/*
 * Copyright 2007-2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Windows.Forms;

namespace OpenRA
{
	public static class FatalErrorDialog
	{
		public static void Show()
		{
			var form = new Form
			{
				Size = new Size(315, 140),
				Text = "Fatal Error",
				MinimizeBox = false,
				MaximizeBox = false,
				FormBorderStyle = FormBorderStyle.FixedDialog,
				StartPosition = FormStartPosition.CenterScreen
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
				Game.Settings.Debug.ShowFatalErrorDialog = !dontShowAgain.Checked;
				Game.Settings.Save();
			};

			SystemSounds.Exclamation.Play();
			form.ShowDialog();
		}

		static void ViewLogsClicked(object sender, EventArgs e)
		{
			try
			{
				Process.Start(Log.LogPath);
			}
			catch { }
		}

		static void ViewFaqClicked(object sender, EventArgs e)
		{
			try
			{
				Process.Start(Game.Settings.Debug.FatalErrorDialogFaq);
			}
			catch { }
		}
	}
}
