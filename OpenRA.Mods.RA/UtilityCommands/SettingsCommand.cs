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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenRA.Mods.RA.UtilityCommands
{
	class SettingsCommand : IUtilityCommand
	{
		public string Name { get { return "--settings-value"; } }

		[Desc("KEY", "Get value of KEY from settings.yaml")]
		public void Run(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Error: Invalid syntax");
				return;
			}

			var section = args[1].Split('.')[0];
			var field = args[1].Split('.')[1];
			var settings = new Settings(Platform.SupportDir + "settings.yaml", Arguments.Empty);
			var result = settings.Sections[section].GetType().GetField(field).GetValue(settings.Sections[section]);
			Console.WriteLine(result);
		}
	}
}
