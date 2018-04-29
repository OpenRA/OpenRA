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
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.UpdateRules;

namespace OpenRA.Mods.Common.UtilityCommands
{
	using YamlFileSet = List<Tuple<IReadWritePackage, string, List<MiniYamlNode>>>;

	class UpdateMapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name { get { return "--update-map"; } }

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return args.Length >= 2;
		}

		[Desc("MAP SOURCE [--detailed] [--apply]", "Updates a map to the latest engine version. SOURCE is either a known tag or the name of an update rule.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;

			// HACK: We know that maps can only be oramap or folders, which are ReadWrite
			var package = new Folder(".").OpenPackage(args[1], modData.ModFiles) as IReadWritePackage;
			if (package == null)
				throw new FileNotFoundException(args[1]);

			IEnumerable<UpdateRule> rules = null;
			if (args.Length > 2)
				rules = UpdatePath.FromSource(modData.ObjectCreator, args[2]);

			if (rules == null)
			{
				Console.WriteLine("--update-map MAP SOURCE [--detailed] [--apply]");

				if (args.Length > 2)
					Console.WriteLine("Unknown source: " + args[2]);

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
				ApplyRules(modData, package, rules);
			else
				UpdateModCommand.PrintSummary(rules, args.Contains("--detailed"));
		}

		static void ApplyRules(ModData modData, IReadWritePackage mapPackage, IEnumerable<UpdateRule> rules)
		{
			var externalFilenames = new HashSet<string>();
			foreach (var rule in rules)
			{
				Console.WriteLine("{0}: {1}", rule.GetType().Name, rule.Name);
				var mapFiles = new YamlFileSet();
				var manualSteps = new List<string>();

				Console.Write("   Updating map... ");

				try
				{
					manualSteps = UpdateUtils.UpdateMap(modData, mapPackage, rule, out mapFiles, externalFilenames);
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

				// Files are saved after each successful automated rule update
				mapFiles.Save();
				Console.WriteLine("COMPLETE");

				if (manualSteps.Any())
				{
					Console.WriteLine("   Manual changes are required to complete this update:");
					foreach (var manualStep in manualSteps)
						Console.WriteLine("    * " + manualStep.Replace("\n", "\n      "));
				}

				Console.WriteLine();
			}

			if (externalFilenames.Any())
			{
				Console.WriteLine("The following shared yaml files referenced by the map have been ignored:");
				Console.WriteLine(UpdateUtils.FormatMessageList(externalFilenames));
				Console.WriteLine("These files are assumed to have already been updated by the --update-mod command");
				Console.WriteLine();
			}

			Console.WriteLine("Semi-automated update complete.");
			Console.WriteLine("Please review the messages above for any manual actions that must be applied.");
		}
	}
}
