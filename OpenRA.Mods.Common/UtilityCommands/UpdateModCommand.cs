#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.UpdateRules;

namespace OpenRA.Mods.Common.UtilityCommands
{
	using YamlFileSet = List<Tuple<IReadWritePackage, string, List<MiniYamlNode>>>;

	class UpdateModCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--update-mod"; } }

		bool IUtilityCommand.ValidateArguments(string[] args) { return true; }

		[Desc("SOURCE [--detailed] [--apply] [--skip-maps]", "Updates a mod to the latest version. SOURCE is either a known tag or the name of an update rule.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			IEnumerable<UpdateRule> rules = null;
			if (args.Length > 1)
				rules = UpdatePath.FromSource(modData.ObjectCreator, args[1]);

			if (rules == null)
			{
				Console.WriteLine("--update-mod SOURCE [--detailed] [--apply] [--skip-maps]");

				if (args.Length > 1)
					Console.WriteLine("Unknown source: " + args[1]);

				Console.WriteLine("Valid sources are:");

				// Print known tags
				Console.WriteLine("   Update Paths:");
				foreach (var p in UpdatePath.KnownPaths)
					Console.WriteLine("      " + p);

				// Print known rules
				Console.WriteLine("   Individual Rules:");
				foreach (var r in UpdatePath.KnownRules(modData.ObjectCreator))
					Console.WriteLine("      " + r);

				return;
			}

			if (args.Contains("--apply"))
			{
				if (!args.Contains("--yes"))
				{
					Console.WriteLine("WARNING: This command will automatically rewrite your mod rules.");
					Console.WriteLine("Side effects of this command may include changing the whitespace to ");
					Console.WriteLine("match the default conventions, and any yaml comments will be removed.");
					Console.WriteLine();
					Console.WriteLine("We strongly recommend that you have a backup of your mod rules, and ");
					Console.WriteLine("for best results, to use a Git client to review the line-by-line ");
					Console.WriteLine("changes and discard any unwanted side effects.");
					Console.WriteLine();
					Console.Write("Press y to continue, or any other key to cancel: ");

					var confirmKey = Console.ReadKey().KeyChar;
					Console.WriteLine();

					if (confirmKey != 'y')
					{
						Console.WriteLine("Update cancelled.");
						return;
					}
				}

				ApplyRules(modData, rules, args.Contains("--skip-maps"));
			}
			else
				PrintSummary(rules, args.Contains("--detailed"));
		}

		public static void PrintSummary(IEnumerable<UpdateRule> rules, bool detailed)
		{
			var count = rules.Count();
			if (count == 1)
				Console.WriteLine("Found 1 API change:");
			else
				Console.WriteLine("Found {0} API changes:", count);

			foreach (var rule in rules)
			{
				Console.WriteLine("  * {0}: {1}", rule.GetType().Name, rule.Name);
				if (detailed)
				{
					Console.WriteLine("     " + rule.Description.Replace("\n", "\n     "));
					Console.WriteLine();
				}
			}

			if (!detailed)
			{
				Console.WriteLine();
				Console.WriteLine("Run this command with the --detailed flag to view detailed information on each change.");
			}

			Console.WriteLine("Run this command with the --apply flag to apply the update rules.");
		}

		static void ApplyRules(ModData modData, IEnumerable<UpdateRule> rules, bool skipMaps)
		{
			Console.WriteLine();

			var externalFilenames = new HashSet<string>();
			foreach (var rule in rules)
			{
				var manualSteps = new List<string>();
				var allFiles = new YamlFileSet();

				Console.WriteLine("{0}: {1}", rule.GetType().Name, rule.Name);

				try
				{
					Console.Write("   Updating mod... ");
					manualSteps.AddRange(UpdateUtils.UpdateMod(modData, rule, out allFiles, externalFilenames));
					Console.WriteLine("COMPLETE");
				}
				catch (Exception ex)
				{
					Console.WriteLine("FAILED");

					Console.WriteLine();
					Console.WriteLine("   The automated changes for this rule were not applied because of an error.");
					Console.WriteLine("   After the issue reported below is resolved you should run the updater");
					Console.WriteLine("   with SOURCE set to {0} to retry these changes", rule.GetType().Name);
					Console.WriteLine();
					Console.WriteLine("   The exception reported was:");
					Console.WriteLine("     " + ex.ToString().Replace("\n", "\n     "));

					continue;
				}

				Console.Write("   Updating system maps... ");

				if (!skipMaps)
				{
					var mapsFailed = false;
					var mapExternalFilenames = new HashSet<string>();
					foreach (var package in modData.MapCache.EnumerateMapPackagesWithoutCaching())
					{
						try
						{
							YamlFileSet mapFiles;
							var mapSteps = UpdateUtils.UpdateMap(modData, package, rule, out mapFiles, mapExternalFilenames);
							allFiles.AddRange(mapFiles);

							if (mapSteps.Any())
								manualSteps.Add("Map: " + package.Name + ":\n" + UpdateUtils.FormatMessageList(mapSteps));
						}
						catch (Exception ex)
						{
							Console.WriteLine("FAILED");

							Console.WriteLine();
							Console.WriteLine("   The automated changes for this rule were not applied because of an error.");
							Console.WriteLine("   After the issue reported below is resolved you should run the updater");
							Console.WriteLine("   with SOURCE set to {0} to retry these changes", rule.GetType().Name);
							Console.WriteLine();
							Console.WriteLine("   The map that caused the error was:");
							Console.WriteLine("     " + package.Name);
							Console.WriteLine();
							Console.WriteLine("   The exception reported was:");
							Console.WriteLine("     " + ex.ToString().Replace("\n", "\n     "));
							mapsFailed = true;
							break;
						}
					}

					if (mapsFailed)
						continue;

					Console.WriteLine("COMPLETE");
				}
				else
					Console.WriteLine("SKIPPED");

				// Files are saved after each successful automated rule update
				allFiles.Save();

				if (manualSteps.Any())
				{
					Console.WriteLine("   Manual changes are required to complete this update:");
					Console.WriteLine(UpdateUtils.FormatMessageList(manualSteps, 1));
				}

				Console.WriteLine();
			}

			if (externalFilenames.Any())
			{
				Console.WriteLine("The following external mod files have been ignored:");
				Console.WriteLine(UpdateUtils.FormatMessageList(externalFilenames));
				Console.WriteLine("These files should be updated by running --update-mod on the referenced mod(s)");
				Console.WriteLine();
			}

			Console.WriteLine("Semi-automated update complete.");
			Console.WriteLine("Please review the messages above for any manual actions that must be applied.");
		}
	}
}
