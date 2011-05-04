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
		
		public void PromptFilepathAsync(string title, Action<string> withPath)
		{
			ExecuteUtility("--display-filepicker \"{0}\"".F(title), withPath);
		}
	
		void ExecuteUtility(string args, Action<string> onComplete)
		{
			Process p = new Process();
			p.StartInfo.FileName = Utility;
			p.StartInfo.Arguments = "{0} --SupportDir \"{1}\"".F(args, Game.SupportDir);
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
	}
}
