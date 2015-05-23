#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Globalization;
using System.Windows.Forms;

namespace OpenRA.Editor
{
	static class Program
	{
		public static Ruleset Rules;

		[STAThread]
		static void Main(string[] args)
		{
			Log.AddChannel("perf", null);

			Application.CurrentCulture = CultureInfo.InvariantCulture;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			Application.Run(new Form1(args));
		}
	}
}
