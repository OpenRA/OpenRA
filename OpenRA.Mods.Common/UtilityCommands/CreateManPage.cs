#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Linq;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class CreateManPage : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--man-page"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Create a man page in troff format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Console.WriteLine(".TH OPENRA 6");
			Console.WriteLine(".SH NAME");
			Console.WriteLine("openra \\- An Open Source modernization of the early 2D Command & Conquer games.");
			Console.WriteLine(".SH SYNOPSIS");
			Console.WriteLine(".B openra");
			Console.WriteLine("[\\fB\\Game.Mod=\\fR\\fImodchooser\\fR]");
			Console.WriteLine(".SH DESCRIPTION");
			Console.WriteLine(".B openra");
			Console.WriteLine("starts the game.");
			Console.WriteLine(".SH OPTIONS");

			var sections = Game.Settings.Sections;
			sections.Add("Launch", new LaunchArguments(new Arguments(new string[0])));
			foreach (var section in sections.OrderBy(s => s.Key))
			{
				var fields = section.Value.GetType().GetFields();
				foreach (var field in fields)
				{
					if (!field.HasAttribute<DescAttribute>())
						continue;

					Console.WriteLine(".TP");

					Console.Write(".BR {0}.{1}=".F(section.Key, field.Name));
					var value = field.GetValue(section.Value);
					if (value != null && !value.ToString().StartsWith("System."))
						Console.WriteLine("\\fI{0}\\fR".F(value));
					else
						Console.WriteLine();

					var lines = field.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
					foreach (var line in lines)
						Console.WriteLine(line);
				}
			}

			Console.WriteLine(".SH FILES");
			Console.WriteLine("Settings are stored in the ~/.openra user folder.");
			Console.WriteLine(".SH BUGS");
			Console.WriteLine("Known issues are tracked at http://bugs.openra.net");
			Console.WriteLine(".SH COPYRIGHT");
			Console.WriteLine("Copyright 2007-2015 The OpenRA Developers (see AUTHORS)");
			Console.WriteLine("This manual is part of OpenRA, which is free software. It is GNU GPL v3 licensed. See COPYING for details.");
		}
	}
}
