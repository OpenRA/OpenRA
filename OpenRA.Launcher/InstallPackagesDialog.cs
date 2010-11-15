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
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO.Pipes;

namespace OpenRA.Launcher
{
	public partial class InstallPackagesDialog : Form
	{
		string mod;
		public InstallPackagesDialog(string mod)
		{
			InitializeComponent();
			Util.UacShield(button2);
			this.mod = mod;
			if (mod == "cnc") radioButton2.Enabled = false;
			label1.Text = string.Format("In order to play OpenRA with the mod \"{0}\", you must install the original game files. " + 
				"These can either be downloaded or for some mods be installed from a CD copy of the original game. " +
				"Please select your choice below and click continue", mod);
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (radioButton1.Checked)
			{
				radioPanel.Visible = false;
				progressPanel.Visible = true;
				button2.Enabled = false;
				cancelButton.Enabled = false;
				backgroundWorker1.RunWorkerAsync();
			}

			if (radioButton2.Checked && folderBrowserDialog1.ShowDialog() == DialogResult.OK)
			{
				var p = UtilityProgram.CallWithAdmin(string.Format("--install-{0}-packages", mod), 
						folderBrowserDialog1.SelectedPath + Path.DirectorySeparatorChar);

				NamedPipeClientStream pipe = new NamedPipeClientStream(".", "OpenRA.Utility", PipeDirection.In);
				pipe.Connect();

				p.WaitForExit();

				using (var response = new StreamReader(pipe))
				{
					string s = response.ReadToEnd();
					if (Util.IsError(ref s))
						DialogResult = DialogResult.No;
					else
						DialogResult = DialogResult.OK;
				}
				Close();
			}
		}

		private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			var p = UtilityProgram.CallWithAdmin("--download-packages", mod);
			Regex r = new Regex(@"(\d{1,3})% (\d+/\d+ bytes)");

			NamedPipeClientStream pipe = new NamedPipeClientStream(".", "OpenRA.Utility", PipeDirection.In);
			pipe.Connect();

			using (var response = new StreamReader(pipe))
			{
				while (!p.HasExited)
				{
					string s = response.ReadLine();
					if (Util.IsError(ref s))
					{
						e.Cancel = true;
						e.Result = s;
						return;
					}
					if (!r.IsMatch(s)) continue;
					var m = r.Match(s);
					backgroundWorker1.ReportProgress(int.Parse(m.Groups[1].Value), m.Groups[2].Value);
				}
			}
		}

		private void backgroundWorker1_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e)
		{
			progressBar1.Value = e.ProgressPercentage;
			progressLabel.Text = (string)e.UserState;
		}

		private void backgroundWorker1_RunWorkerCompleted(object sender, System.ComponentModel.RunWorkerCompletedEventArgs e)
		{
			if (e.Cancelled)
			{
				MessageBox.Show((string)e.Result);
				DialogResult = DialogResult.No;
			}
			else
				DialogResult = DialogResult.OK;
			progressPanel.Visible = false;
			radioPanel.Visible = true;
			Close();
		}
	}
}
