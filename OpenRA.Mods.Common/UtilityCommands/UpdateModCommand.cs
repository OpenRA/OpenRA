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
	using YamlFileSet = List<(IReadWritePackage, string, List<MiniYamlNode>)>;

	class UpdateModCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--update-mod";

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
			{
				if (!args.Contains("--yes"))
				{
					Console.WriteLine("WARNING: This command will automatically rewrite your mod rules.");
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

		static void Log(StreamWriter logWriter, string format, params object[] args)
		{
			logWriter.Write(format, args);
			Console.Write(format, args);
		}

		static void LogLine(StreamWriter logWriter, string format, params object[] args)
		{
			logWriter.WriteLine(format, args);
			Console.WriteLine(format, args);
		}

		static void LogLine(StreamWriter logWriter)
		{
			logWriter.WriteLine();
			Console.WriteLine();
		}

		static void ApplyRules(ModData modData, IEnumerable<UpdateRule> rules, bool skipMaps)
		{
			Console.WriteLine();

			var logWriter = File.CreateText("update.log");
			logWriter.AutoFlush = true;

			var externalFilenames = new HashSet<string>();
			foreach (var rule in rules)
			{
				var manualSteps = new List<string>();
				YamlFileSet allFiles;

				LogLine(logWriter, $"{rule.GetType().Name}: {rule.Name}");

				try
				{
					Log(logWriter, "   Updating mod... ");
					manualSteps.AddRange(UpdateUtils.UpdateMod(modData, rule, out allFiles, externalFilenames));
					LogLine(logWriter, "COMPLETE");
				}
				catch (Exception ex)
				{
					Console.WriteLine("FAILED");

					LogLine(logWriter);
					LogLine(logWriter, "   The automated changes for this rule were not applied because of an error.");
					LogLine(logWriter, "   After the issue reported below is resolved you should run the updater");
					LogLine(logWriter, "   with SOURCE set to {0} to retry these changes", rule.GetType().Name);
					LogLine(logWriter);
					LogLine(logWriter, "   The exception reported was:");
					LogLine(logWriter, "     " + ex.ToString().Replace("\n", "\n     "));

					continue;
				}

				Log(logWriter, "   Updating system maps... ");

				if (!skipMaps)
				{
					var mapsFailed = false;
					var mapExternalFilenames = new HashSet<string>();
					foreach (var package in modData.MapCache.EnumerateMapPackagesWithoutCaching())
					{
						try
						{
							var mapSteps = UpdateUtils.UpdateMap(modData, package, rule, out var mapFiles, mapExternalFilenames);
							allFiles.AddRange(mapFiles);

							if (mapSteps.Count > 0)
								manualSteps.Add("Map: " + package.Name + ":\n" + UpdateUtils.FormatMessageList(mapSteps));
						}
						catch (Exception ex)
						{
							LogLine(logWriter, "FAILED");
							LogLine(logWriter);
							LogLine(logWriter, "   The automated changes for this rule were not applied because of an error.");
							LogLine(logWriter, "   After the issue reported below is resolved you should run the updater");
							LogLine(logWriter, "   with SOURCE set to {0} to retry these changes", rule.GetType().Name);
							LogLine(logWriter);
							LogLine(logWriter, "   The map that caused the error was:");
							LogLine(logWriter, "     " + package.Name);
							LogLine(logWriter);
							LogLine(logWriter, "   The exception reported was:");
							LogLine(logWriter, "     " + ex.ToString().Replace("\n", "\n     "));
							mapsFailed = true;
							break;
						}
					}

					if (mapsFailed)
						continue;

					LogLine(logWriter, "COMPLETE");
				}
				else
					LogLine(logWriter, "SKIPPED");

				// Files are saved after each successful automated rule update
				allFiles.Save();

				if (manualSteps.Count > 0)
				{
					LogLine(logWriter, "   Manual changes are required to complete this update:");
					LogLine(logWriter, UpdateUtils.FormatMessageList(manualSteps, 1));
				}

				LogLine(logWriter);
			}

			if (externalFilenames.Count > 0)
			{
				LogLine(logWriter, "The following external mod files have been ignored:");
				LogLine(logWriter, UpdateUtils.FormatMessageList(externalFilenames));
				LogLine(logWriter, "These files should be updated by running --update-mod on the referenced mod(s)");
				LogLine(logWriter);
			}

			Console.WriteLine("Semi-automated update complete.");
			Console.WriteLine("Please review the messages above for any manual actions that must be applied.");
			Console.WriteLine("These messages have also been written to an update.log file in the current directory.");
		}
	}
}
