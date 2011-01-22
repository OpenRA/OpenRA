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
using System.Diagnostics;
using System.Threading;

namespace OpenRA
{
	public class Utilities
	{
		readonly string Utility;
		
		public Utilities(string utility)
		{
			Utility = utility;
		}
	
		public void ExtractZipAsync(string zipFile, string path, Action<string> parseOutput, Action onComplete)
		{
			ExecuteUtilityAsync("--extract-zip \"{0}\" \"{1}\"".F(zipFile, path), parseOutput, onComplete);
		}
		
		public void InstallRAFilesAsync(string cdPath, Action<string> parseOutput, Action onComplete)
		{
			ExecuteUtilityAsync("--install-ra-packages \"{0}\"".F(cdPath), parseOutput, onComplete);
		}
		
		public void PromptFilepathAsync(string title, Action<string> withPath)
		{
			ExecuteUtility("--display-filepicker \"{0}\"".F(title), withPath);
		}
	
		void ExecuteUtility(string args, Action<string> onComplete)
		{
			Process p = new Process();
			p.StartInfo.FileName = Utility;
			p.StartInfo.Arguments = args;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.RedirectStandardOutput = true;
			p.EnableRaisingEvents = true;
			p.Exited += (_,e) =>
			{
				onComplete(p.StandardOutput.ReadToEnd().Trim());
			};
			p.Start();
		}
		
		void ExecuteUtilityAsync(string args, Action<string> parseOutput, Action onComplete)
		{
			Process p = new Process();
			p.StartInfo.FileName = Utility;
			p.StartInfo.Arguments = args;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.CreateNoWindow = true;
			p.StartInfo.RedirectStandardOutput = true;
			p.Start();
			
			var t = new Thread( _ =>
			{
				using (var reader = p.StandardOutput)
				{
					// This is wrong, chrisf knows why
					while (!p.HasExited)
					{
						string s = reader.ReadLine();
						if (string.IsNullOrEmpty(s)) continue;
						parseOutput(s);
					}
				}
				onComplete();
			}) { IsBackground = true };
			t.Start();	
		}
	}
}
