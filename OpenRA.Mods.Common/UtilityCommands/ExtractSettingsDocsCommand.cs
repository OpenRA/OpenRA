#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
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
	class ExtractSettingsDocsCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--settings-docs";

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("[VERSION]", "Generate settings documentation in markdown format.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			Game.ModData = utility.ModData;

			var version = utility.ModData.Manifest.Metadata.Version;
			if (args.Length > 1)
				version = args[1];

			Console.WriteLine(
				"This documentation displays annotated settings with default values and description. " +
				"Please do not edit it directly, but add new `[Desc(\"String\")]` tags to the source code. This file has been " +
				"automatically generated for version {0} of OpenRA.", version);
			Console.WriteLine();
			Console.WriteLine("All settings can be changed by starting the game via a command-line parameter like `Game.Mod=ra`.");
			Console.WriteLine();
			Console.WriteLine("## Location");
			Console.WriteLine("* Windows: `%APPDATA%\\OpenRA\\settings.yaml`");
			Console.WriteLine("* Mac OS X: `~/Library/Application Support/OpenRA/settings.yaml`");
			Console.WriteLine("* Linux `~/.config/openra/settings.yaml`");
			Console.WriteLine();
			Console.WriteLine(
				"Older releases (before playtest-20190825) used different locations, " +
				"which newer versions may continue to use in some circumstances:");
			Console.WriteLine("* Windows: `%USERPROFILE%\\Documents\\OpenRA\\settings.yaml`");
			Console.WriteLine("* Linux `~/.openra/settings.yaml`");
			Console.WriteLine();
			Console.WriteLine(
				"If you create the folder `Support` relative to the OpenRA main directory, everything " +
				"including settings gets stored there to aid portable installations.");
			Console.WriteLine();

			var sections = new Settings(null, new Arguments()).Sections;
			foreach (var section in sections.OrderBy(s => s.Key))
			{
				var fields = section.Value.GetType().GetFields();
				if (fields.Any(field => field.GetCustomAttributes<DescAttribute>(false).Length > 0))
					Console.WriteLine($"## {section.Key}");
				else
					Console.WriteLine();

				foreach (var field in fields)
				{
					if (!field.HasAttribute<DescAttribute>())
						continue;

					Console.WriteLine($"### {field.Name}");
					var lines = field.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
					foreach (var line in lines)
					{
						Console.WriteLine(line);
						Console.WriteLine();
					}

					var value = field.GetValue(section.Value);
					if (value != null && !value.ToString().StartsWith("System."))
					{
						Console.WriteLine("**Default Value:** {0}", value);
						Console.WriteLine();
						Console.WriteLine("```miniyaml");
						Console.WriteLine($"{section.Key}: ");
						Console.WriteLine($"\t{field.Name}: {value}");
						Console.WriteLine("```");
					}
					else
						Console.WriteLine();
				}
			}
		}
	}
}
