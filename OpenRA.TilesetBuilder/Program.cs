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
using System.Linq;
using System.Windows.Forms;

namespace OpenRA.TilesetBuilder
{
	static class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			if (args.Length < 1)
			{
				Application.Run(new FormBuilder("", "0", false, "Tilesets"));
			}
			else
			{
				if (args.Contains("--export"))
					Application.Run(new FormBuilder(args[0], args[1], true, args[3]));
				else
					Application.Run(new FormBuilder(args[0], args[1], false, "Tilesets"));
			}
		}
	}
}
