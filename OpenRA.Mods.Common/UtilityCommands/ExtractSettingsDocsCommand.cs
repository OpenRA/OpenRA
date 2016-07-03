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
	class ExtractSettingsDocsCommand : IUtilityCommand
	{
		public string Name { get { return "--settings-docs"; } }

		public bool ValidateArguments(string[] args)
		{
			return true;
		}

		[Desc("Generate settings documentation in markdown format.")]
		public void Run(ModData modData, string[] args)
		{
			Game.ModData = modData;
			Console.WriteLine(
				"This documentation is aimed at server administrators. It displays all settings with default values and description. " +
				"Please do not edit it directly, but add new `[Desc(\"String\")]` tags to the source code. This file has been " +
				"automatically generated for version {0} of OpenRA.", Game.ModData.Manifest.Mod.Version);
			Console.WriteLine();
			Console.WriteLine("All settings can be changed by starting the game via a command-line parameter like `Game.Mod=ra`.");
			Console.WriteLine();
			Console.WriteLine("## Location");
			Console.WriteLine("* Windows: `My Documents\\OpenRA\\settings.yaml`");
			Console.WriteLine("* Mac OS X: `~/Library/Application Support/OpenRA/settings.yaml`");
			Console.WriteLine("* Linux `~/.openra/settings.yaml`");
			Console.WriteLine();
			Console.WriteLine(
				"If you create the folder `Support` relative to the OpenRA main directory, everything " +
				"including settings gets stored there to aid portable installations.");
			Console.WriteLine();

			var sections = new Settings(null, new Arguments()).Sections;
			sections.Add("Launch", new LaunchArguments(new Arguments(new string[0])));
			foreach (var section in sections.OrderBy(s => s.Key))
			{
				var fields = section.Value.GetType().GetFields();
				if (fields.Length > 0 && fields.Where(field => field.GetCustomAttributes<DescAttribute>(false).Length > 0).Count() > 0)
					Console.WriteLine("## {0}", section.Key);
				else
					Console.WriteLine();

				foreach (var field in fields)
				{
					if (!field.HasAttribute<DescAttribute>())
						continue;

					Console.WriteLine("### {0}", field.Name);
					var lines = field.GetCustomAttributes<DescAttribute>(false).SelectMany(d => d.Lines);
					foreach (var line in lines)
					{
						Console.WriteLine("{0}", line);
						Console.WriteLine();
					}

					var value = field.GetValue(section.Value);
					if (value != null && !value.ToString().StartsWith("System."))
					{
						Console.WriteLine("**Default Value:** {0}", value);
						Console.WriteLine();
						Console.WriteLine("```yaml");
						Console.WriteLine("{0}: ", section.Key);
						Console.WriteLine("\t{0}: {1}", field.Name, value);
						Console.WriteLine("```  ");
					}
					else
						Console.WriteLine();
				}
			}
		}
	}
}
