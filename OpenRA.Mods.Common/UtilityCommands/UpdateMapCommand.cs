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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.UpdateRules;

namespace OpenRA.Mods.Common.UtilityCommands
{
	class UpdateMapCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--update-map";

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
			var folder = new Folder(Platform.EngineDir);
			if (!(folder.OpenPackage(args[1], modData.ModFiles) is IReadWritePackage package))
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

				var ruleGroups = new Dictionary<string, List<string>>();

				// Print known tags
				Console.WriteLine("   Update Paths:");
				foreach (var p in UpdatePath.KnownPaths)
				{
					Console.WriteLine("      " + p);
					ruleGroups[p] = UpdatePath.FromSource(modData.ObjectCreator, p, false)
						.Select(r => r.GetType().Name)
						.Where(r => !ruleGroups.Values.Any(g => g.Contains(r)))
						.ToList();
				}

				// Print known rules
				Console.WriteLine("   Individual Rules:");
				foreach (var kv in ruleGroups)
				{
					if (kv.Value.Count == 0)
						continue;

					Console.WriteLine("      " + kv.Key + ":");
					foreach (var r in kv.Value)
						Console.WriteLine("         " + r);
				}

				var other = UpdatePath.KnownRules(modData.ObjectCreator)
					.Where(r => !ruleGroups.Values.Any(g => g.Contains(r)));

				if (other.Any())
				{
					Console.WriteLine("      Other:");
					foreach (var r in other)
						Console.WriteLine("         " + r);
				}

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
				Console.WriteLine($"{rule.GetType().Name}: {rule.Name}");
				Console.Write("   Updating map... ");

				try
				{
					var manualSteps = UpdateUtils.UpdateMap(modData, mapPackage, rule, out var mapFiles, externalFilenames);

					// Files are saved after each successful automated rule update
					mapFiles.Save();
					Console.WriteLine("COMPLETE");

					if (manualSteps.Count > 0)
					{
						Console.WriteLine("   Manual changes are required to complete this update:");
						foreach (var manualStep in manualSteps)
							Console.WriteLine("    * " + manualStep.Replace("\n", "\n      "));
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("FAILED");

					Console.WriteLine();
					Console.WriteLine("   The automated changes for this rule were not applied because of an error.");
					Console.WriteLine("   After the issue reported below is resolved you should run the updater");
					Console.WriteLine($"   with SOURCE set to {rule.GetType().Name} to retry these changes");
					Console.WriteLine();
					Console.WriteLine("   The exception reported was:");
					Console.WriteLine("     " + ex.ToString().Replace("\n", "\n     "));
					continue;
				}

				Console.WriteLine();
			}

			if (externalFilenames.Count > 0)
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
