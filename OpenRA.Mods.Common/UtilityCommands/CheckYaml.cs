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
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Graphics;
using OpenRA.Mods.Common.Lint;

namespace OpenRA.Mods.Common.UtilityCommands
{
	sealed class CheckYaml : IUtilityCommand
	{
		string IUtilityCommand.Name => "--check-yaml";

		static int errors = 0;

		// mimic Windows compiler error format
		static void EmitError(string e)
		{
			Console.WriteLine($"OpenRA.Utility(1,1): Error: {e}");
			++errors;
		}

		static void EmitWarning(string e)
		{
			Console.WriteLine($"OpenRA.Utility(1,1): Warning: {e}");
		}

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return true;
		}

		bool warningAsError = false;

		[Desc("[MAPFILE]", "Check a mod or map for certain yaml errors.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			// HACK: The engine code assumes that Game.modData is set.
			var modData = Game.ModData = utility.ModData;
			warningAsError = Environment.GetEnvironmentVariable("TREAT_WARNINGS_AS_ERRORS")?.Equals("true", StringComparison.CurrentCultureIgnoreCase) ?? false;

			try
			{
				Log.AddChannel("debug", null);
				Log.AddChannel("perf", null);

				// bind some nonfatal error handling into FieldLoader, so we don't just *explode*.
				ObjectCreator.MissingTypeAction = s => EmitError($"Missing Type: {s}.");
				FieldLoader.UnknownFieldAction = (s, f) => EmitError($"FieldLoader: Missing field `{s}` on `{f.Name}`.");

				var maps = new List<(IReadWritePackage Package, string Map)>();
				if (args.Length < 2)
				{
					Console.WriteLine($"Testing mod: {modData.Manifest.Metadata.Title}");

					// Run all rule checks on the default mod rules.
					CheckRules(modData, modData.DefaultRules);
					foreach (var tileset in modData.DefaultTerrainInfo.Keys)
					{
						Console.WriteLine($"Testing default sequences for {tileset}");

						var sequences = new SequenceSet(modData.DefaultFileSystem, modData, tileset, null);
						CheckSequences(modData, modData.DefaultRules, sequences);
					}

					// Run all generic (not mod-level) checks here.
					foreach (var customPassType in modData.ObjectCreator.GetTypesImplementing<ILintPass>())
					{
						try
						{
							var customPass = (ILintPass)modData.ObjectCreator.CreateBasic(customPassType);
							customPass.Run(EmitError, warningAsError ? EmitError : EmitWarning, modData);
						}
						catch (Exception e)
						{
							EmitError($"{customPassType} failed with exception: {e}");
						}
					}

					// Use all system maps for lint checking
					maps = modData.MapCache.EnumerateMapDirPackagesAndNames().ToList();
				}
				else
					maps.Add((new Folder(Platform.EngineDir), args[1]));

				foreach (var map in maps)
				{
					var package = map.Package.OpenPackage(map.Map, modData.ModFiles);
					if (package == null)
						continue;

					using (var testMap = new Map(modData, package))
						TestMap(testMap, modData);
				}

				if (errors > 0)
				{
					Console.WriteLine($"Errors: {errors}");
					Environment.Exit(1);
				}
			}
			catch (Exception e)
			{
				EmitError($"Failed with exception: {e}");
				Environment.Exit(1);
			}
		}

		void TestMap(Map map, ModData modData)
		{
			Console.WriteLine($"Testing map: {map.Title}");

			// Lint tests can't be trusted if the map rules are bogus
			// so report that problem then skip the tests
			if (map.InvalidCustomRules)
			{
				EmitError(map.InvalidCustomRulesException.ToString());
				return;
			}

			// Run all rule checks on the map if it defines custom rules.
			if (map.RuleDefinitions != null || map.VoiceDefinitions != null || map.WeaponDefinitions != null)
			{
				CheckRules(modData, map.Rules);
				if (map.SequenceDefinitions != null)
					CheckSequences(modData, modData.DefaultRules, map.Sequences);
			}

			// Run all map-level checks here.
			foreach (var customMapPassType in modData.ObjectCreator.GetTypesImplementing<ILintMapPass>())
			{
				try
				{
					var customMapPass = (ILintMapPass)modData.ObjectCreator.CreateBasic(customMapPassType);
					customMapPass.Run(EmitError, warningAsError ? EmitError : EmitWarning, modData, map);
				}
				catch (Exception e)
				{
					EmitError($"{customMapPassType} failed with exception: {e}");
				}
			}
		}

		void CheckRules(ModData modData, Ruleset rules)
		{
			foreach (var customRulesPassType in modData.ObjectCreator.GetTypesImplementing<ILintRulesPass>())
			{
				try
				{
					var customRulesPass = (ILintRulesPass)modData.ObjectCreator.CreateBasic(customRulesPassType);
					customRulesPass.Run(EmitError, warningAsError ? EmitError : EmitWarning, modData, rules);
				}
				catch (Exception e)
				{
					EmitError($"{customRulesPassType} failed with exception: {e}");
				}
			}
		}

		void CheckSequences(ModData modData, Ruleset rules, SequenceSet sequences)
		{
			foreach (var customSequencesPassType in modData.ObjectCreator.GetTypesImplementing<ILintSequencesPass>())
			{
				try
				{
					var customRulesPass = (ILintSequencesPass)modData.ObjectCreator.CreateBasic(customSequencesPassType);
					customRulesPass.Run(EmitError, warningAsError ? EmitError : EmitWarning, modData, rules, sequences);
				}
				catch (Exception e)
				{
					EmitError($"{customSequencesPassType} failed with exception: {e}");
				}
			}
		}
	}
}
