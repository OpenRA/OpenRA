#region Copyright & License Information
/*
 * Copyright 2013 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Net;
using OpenRA.FileFormats;

namespace OpenRA.Support
{
	public class CrashReporter : Form
	{
		private CrashReport Report;
		private Button BtnSubmit;
		ProgressBar PBar;

		public static void Run(CrashReport report)
		{
			var cr = new CrashReporter(report);
			Application.Run(cr);
		}

		public CrashReporter(CrashReport report)
		{
			Report = report;
			Text = "OpenRA: Crash Report";
			Size = new Size(550, 400);
			MinimizeBox = false;
			MaximizeBox = false;
			Font = new Font("Sans", 12);
			FormBorderStyle = FormBorderStyle.FixedDialog;
			CenterToScreen();

			Label lblTitle = new Label();
			lblTitle.Text = "We're sorry, OpenRA crashed...";
			lblTitle.Font = new Font("Sans", 14, FontStyle.Bold);
			lblTitle.TextAlign = ContentAlignment.MiddleCenter;
			lblTitle.Width = Size.Width;
			lblTitle.Height = 30;
			lblTitle.Location = new Point(0, 25);
			Controls.Add(lblTitle);

			Label lblText = new Label();
			lblText.Text = @"This should've never happened. You can help us fix this by submitting the generated crash report.";
			lblText.Location = new Point(25, lblTitle.Bottom+25);
			Controls.Add(lblText);

			Label lblTeam = new Label();
			lblTeam.Text = "The OpenRA Development Team";
			lblTeam.Size = new Size(Width-50, 25);
			lblTeam.Location = new Point(0, lblText.Bottom);
			lblTeam.TextAlign = ContentAlignment.TopRight;
			Controls.Add(lblTeam);

			Label lblError = new Label();
			lblError.AutoSize = true;
			lblError.Text = Report.Error;
			lblError.Location = new Point(40, lblTeam.Bottom + 25);
			Controls.Add(lblError);

			Label lblMessage = new Label();
			lblMessage.Size = new Size(Width-80, 50);
			lblMessage.Text = Report.Message;
			lblMessage.Location = new Point(lblError.Left, lblError.Bottom);
			Controls.Add(lblMessage);

			LinkLabel lnkFaq = new LinkLabel();
			lnkFaq.Font = new Font("Sans", 10);
			lnkFaq.AutoSize = true;
			lnkFaq.Text = "Consult the FAQ";
			lnkFaq.LinkArea = new LinkArea(0, lnkFaq.Text.Length);
			lnkFaq.LinkClicked += (sender, e) => {Process.Start("https://github.com/OpenRA/OpenRA/wiki/FAQ");};
			lnkFaq.Dock = DockStyle.Bottom;
			Controls.Add(lnkFaq);

			LinkLabel lnkReport = new LinkLabel();
			lnkReport.Font = new Font("Sans", 10);
			lnkReport.AutoSize = true;
			lnkReport.Text = "Show the Crash Report";
			lnkReport.LinkArea = new LinkArea(0, lnkReport.Text.Length);
			lnkReport.LinkClicked += (sender, e) => {CrashReport.OpenReportsFolder();};
			lnkReport.Dock = DockStyle.Bottom;
			Controls.Add(lnkReport);

			PBar = new ProgressBar();
			PBar.Size = new Size(400, 25);
			PBar.Location = new Point(75, lblMessage.Bottom+20);
			PBar.Minimum = 0;
			PBar.Maximum = 100;
			PBar.Value = 0;
			Controls.Add(PBar);

			BtnSubmit = new Button();
			BtnSubmit.Text = "Submit";
			BtnSubmit.Size = new Size(100, 32);
			BtnSubmit.Location = new Point((Width-BtnSubmit.Width)/2, PBar.Bottom+10);
			BtnSubmit.Click += (sender, e) => { Submit(); };
			Controls.Add(BtnSubmit);
		}

		private void Submit()
		{
			BtnSubmit.Enabled = false;
			Report.Submit(
				(p) => {PBar.Value = p;},
				() =>
				{
					MessageBox.Show("The crash report has been submitted. Thank you for your support!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
					Close();
				},
				(e) =>
				{
					var message = "The crash report could not be submitted.";
					if (e != null)
					{
						var we = e as WebException;
						var resp = (HttpWebResponse) we.Response;
						message += "\n" + we.Message;
						if (resp != null)
							message += "\n {0} - {1}".F(resp.StatusCode, resp.StatusDescription);
					}
					MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
					Close();
				}
			);
		}

	}
}

