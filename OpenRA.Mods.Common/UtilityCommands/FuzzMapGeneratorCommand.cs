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
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UtilityCommands
{
	sealed class FuzzMapGeneratorCommand : IUtilityCommand
	{
		string IUtilityCommand.Name => "--fuzz-map-generator";

		sealed class Configuration
		{
			public const string TilesetVariable = "__tileset__";
			public const string SizeVariable = "__size__";

			public readonly string MapGeneratorType;
			public readonly bool NoDefaults;
			public readonly bool DryRun;
			public readonly long Skip;
			public readonly int Shard;
			public readonly int ShardCount;
			public readonly ImmutableArray<string> Variables;
			public readonly ImmutableDictionary<string, ImmutableArray<string>> Choices;

			Configuration(
				string mapGeneratorName,
				bool noDefaults,
				bool dryRun,
				long skip,
				int shard,
				int shardCount,
				ImmutableArray<string> variables,
				ImmutableDictionary<string, ImmutableArray<string>> choices)
			{
				MapGeneratorType = mapGeneratorName;
				NoDefaults = noDefaults;
				DryRun = dryRun;
				Skip = skip;
				Shard = shard;
				ShardCount = shardCount;
				Variables = variables;
				Choices = choices;
			}

			public static Configuration Parse(string[] args)
			{
				string mapGeneratorName = null;
				var noDefaults = false;
				var dryRun = false;
				long skip = 0;
				var shard = 0;
				var shardCount = 1;
				var variables = new List<string>();
				var choices = new Dictionary<string, ImmutableArray<string>>();

				bool AddVariable(string variable, string choicesStr)
				{
					if (choices.ContainsKey(variable))
						return false;

					variables.Add(variable);
					choices.Add(variable, choicesStr.Split(',').ToImmutableArray());
					return true;
				}

				foreach (var arg in args)
				{
					var parts = arg.Split('=');
					switch (parts[0])
					{
						case "--fuzz-map-generator":
							break;
						case "--no-defaults":
							if (parts.Length != 1)
								return null;

							noDefaults = true;
							break;
						case "--dry-run":
							if (parts.Length != 1)
								return null;

							dryRun = true;
							break;
						case "--skip":
							if (parts.Length != 2 || skip != 0)
								return null;

							if (!Exts.TryParseInt64Invariant(parts[1], out skip))
								return null;

							break;
						case "--shard":
							if (parts.Length != 2 || shardCount != 1)
								return null;

							var shardParts = parts[1].Split('/');

							if (!Exts.TryParseInt32Invariant(shardParts[0], out shard))
								return null;

							if (!Exts.TryParseInt32Invariant(shardParts[1], out shardCount))
								return null;

							if (shard < 0 || shard >= shardCount)
								return null;

							break;
						case "--generator":
							if (parts.Length != 2 || mapGeneratorName != null)
								return null;

							mapGeneratorName = parts[1];
							break;
						case "--tilesets":
							if (parts.Length != 2 || !AddVariable(TilesetVariable, parts[1]))
								return null;

							break;
						case "--sizes":
							if (parts.Length != 2 || !AddVariable(SizeVariable, parts[1]))
								return null;

							break;
						case "--choices":
							if (parts.Length != 3 || !AddVariable(parts[1], parts[2]))
								return null;

							break;
						default:
							Console.Error.WriteLine($"Unrecognized argument {arg}");
							return null;
					}
				}

				if (mapGeneratorName == null)
				{
					Console.Error.WriteLine("--generator is mandatory");
					return null;
				}

				if (!choices.ContainsKey(TilesetVariable))
				{
					Console.Error.WriteLine("--tilesets is mandatory");
					return null;
				}

				if (!choices.ContainsKey(SizeVariable))
				{
					Console.Error.WriteLine("--sizes is mandatory");
					return null;
				}

				return new(
					mapGeneratorName,
					noDefaults,
					dryRun,
					skip,
					shard,
					shardCount,
					variables.ToImmutableArray(),
					choices.ToImmutableDictionary());
			}
		}

		bool IUtilityCommand.ValidateArguments(string[] args)
		{
			return Configuration.Parse(args) != null;
		}

		[Desc(
			"--generator=TYPE " +
			"(--choices=<OPTION>=<CHOICE>,...|--tilesets=<TILESET>,...|--sizes=<WIDTH>x<HEIGHT>,...)... " +
			"[--no-defaults] " +
			"[--dry-run] " +
			"[--skip=COUNT] " +
			"[--shard=START/STEP]",
			"Exercise the specified map generator, iterating through combinations of settings.")]
		void IUtilityCommand.Run(Utility utility, string[] args)
		{
			var config = Configuration.Parse(args);

			// HACK: The engine code assumes that Game.modData is set.
			// HACK: We know that maps can only be oramap or folders, which are ReadWrite
			var modData = Game.ModData = utility.ModData;

			var iteration = new int[config.Variables.Length];
			var iterationLimits = config.Variables
				.Select(variable => config.Choices[variable].Length)
				.ToImmutableArray();

			var generator = modData.DefaultRules.Actors[SystemActors.EditorWorld]
				.TraitInfos<IEditorMapGeneratorInfo>()
				.FirstOrDefault(info => info.Type == config.MapGeneratorType);
			if (generator == null)
				throw new ArgumentException($"No map generator with type `{config.MapGeneratorType}`");

			long maxSerial = 1;
			long tests = 0;
			long failures = 0;
			foreach (var choices in config.Choices.Values)
				maxSerial *= choices.Length;

			var choiceFailureCounters = new Dictionary<string, int>();
			var exceptionCounters = new Dictionary<string, int>();
			var exceptionExamples = new Dictionary<string, string>();

			for (long serial = 0; serial < maxSerial; serial++)
			{
				if (serial >= config.Skip && serial % config.ShardCount == config.Shard)
				{
					Console.Error.Write($"\rCurrent combination {serial} / {maxSerial}, with {failures} / {tests} tests failed so far. ");

					var iterationChoices = iteration
						.Select((choiceI, variableI) =>
							new KeyValuePair<string, string>(
								config.Variables[variableI],
								config.Choices[config.Variables[variableI]][choiceI]))
						.ToDictionary(kv => kv.Key, kv => kv.Value);

					var descriptionBuilder = new StringBuilder();
					foreach (var variable in config.Variables)
						descriptionBuilder.Append(CultureInfo.InvariantCulture, $"  {variable}={iterationChoices[variable]}\n");

					if (!config.NoDefaults)
						descriptionBuilder.Append("  (+Defaults)\n");

					var description = descriptionBuilder.ToString();

					var choiceFailureCounterKeys = iterationChoices
						.Select(kv => $"{kv.Key}={kv.Value}")
						.ToHashSet();

					var terrainInfo = modData.DefaultTerrainInfo[iterationChoices[Configuration.TilesetVariable]];
					iterationChoices.Remove(Configuration.TilesetVariable);

					var size = iterationChoices[Configuration.SizeVariable]
						.Split('x')
						.Select(str =>
							{
								if (Exts.TryParseInt32Invariant(str, out var v))
									return v;
								else
									throw new ArgumentException($"bad map size `{iterationChoices[Configuration.SizeVariable]}`");
							})
						.ToImmutableArray();
					iterationChoices.Remove(Configuration.SizeVariable);
					if (size.Length != 2)
						throw new ArgumentException($"bad map size `{iterationChoices[Configuration.SizeVariable]}`");

					var settings = generator.GetSettings();
					foreach (var o in settings.Options)
					{
						if (iterationChoices.TryGetValue(o.Id, out var choice))
						{
							if (o is MapGeneratorBooleanOption bo)
								bo.Value = FieldLoader.GetValue<bool>("choice", choice);
							else if (o is MapGeneratorIntegerOption io)
								io.Value = FieldLoader.GetValue<int>("choice", choice);
							else if (o is MapGeneratorMultiIntegerChoiceOption mio)
								mio.Value = FieldLoader.GetValue<int>("choice", choice);
							else if (o is MapGeneratorMultiChoiceOption mo)
								mo.Value = choice;

							iterationChoices.Remove(o.Id);
						}
						else if (config.NoDefaults)
							throw new ArgumentException($"No choices specified for option `{o.Id}`");
					}

					if (iterationChoices.Count != 0)
						throw new ArgumentException($"Unknown options: {string.Join(", ", iterationChoices.Keys)}");

					if (!config.DryRun)
					{
						tests++;
						try
						{
							generator.Generate(modData, settings.Compile(terrainInfo, new Size(size[0], size[1])));
						}
						catch (Exception e) when (e is MapGenerationException || e is YamlException)
						{
							failures++;
							var exceptionDescription = $"{e.GetType().Name}: {e.Message}";
							var exceptionStack = e.StackTrace ?? "(null stack trace)";
							Console.Out.Write($"\nMap {serial} failed: {exceptionDescription}\nSettings:\n{description}\n");
							{
								exceptionCounters.TryGetValue(exceptionStack, out var count);
								exceptionCounters[exceptionStack] = count + 1;
								exceptionExamples[exceptionStack] = exceptionDescription;
							}

							foreach (var counterKey in choiceFailureCounterKeys)
							{
								choiceFailureCounters.TryGetValue(counterKey, out var count);
								choiceFailureCounters[counterKey] = count + 1;
							}
						}
					}
				}

				for (var i = 0; i < iteration.Length; i++)
				{
					if (++iteration[i] < iterationLimits[i])
						break;
					else
						iteration[i] = 0;
				}
			}

			Console.Out.Write($"\nDone. {failures} / {tests} tested maps failed.\n");

			if (failures == 0)
				return;

			Console.Out.Write("Most common exceptions grouped by stack trace:\n\n");
			var topExceptions = exceptionCounters
				.OrderByDescending(kv => kv.Value);
			foreach (var (stackTrace, count) in topExceptions)
			{
				var example = exceptionExamples[stackTrace];
				Console.Out.Write($"Count: {count}\nExample message: {example}\nStack trace:\n{stackTrace}\n\n");
			}

			Console.Out.Write("Variable choices ordered by greatest number of failures:\n\n");
			var topChoices = choiceFailureCounters
				.OrderByDescending(kv => kv.Value);
			foreach (var (choice, count) in topChoices)
				Console.Out.Write($"{count}\t{choice}\n");
		}
	}
}
