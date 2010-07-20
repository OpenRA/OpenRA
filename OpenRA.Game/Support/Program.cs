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
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace OpenRA
{
	static class Program
	{
		[STAThread]
		static void Main( string[] args )
		{
			// brutal hack
			Application.CurrentCulture = CultureInfo.InvariantCulture;

			if (Debugger.IsAttached || args.Contains("--just-die"))
			{
				Run(args);
				return;
			}

			try
			{
				Run( args );
			}
			catch( Exception e )
			{
				Log.AddChannel("exception", "openra.exception.txt", true, false);
				Log.Write("exception", "{0}", e.ToString());
				if (!Game.Settings.DeveloperMode || ( Game.Settings.DeveloperMode && Game.GetGameId() != 0) )
					Log.Upload(Game.GetGameId());
				throw;
			}
		}

		static void Run( string[] args )
		{
			Game.Initialize( new Settings( args ) );
			Game.Run();
		}
	}
}