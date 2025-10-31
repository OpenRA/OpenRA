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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.MapGenerator
{
	public abstract class MapGeneratorOption
	{
		[FieldLoader.Ignore]
		public readonly string Id;
		public readonly string Label = null;
		public readonly int Priority = 0;

		protected MapGeneratorOption(string id, MiniYaml yaml)
		{
			Id = id;
			FieldLoader.Load(this, yaml);
		}

		public abstract IReadOnlyCollection<MiniYamlNode> GetSettings(ITerrainInfo terrainInfo, int playerCount);

		public virtual IEnumerable<string> GetFluentReferences()
		{
			if (Label != null)
				yield return Label;
		}
	}

	public class MapGeneratorBooleanOption : MapGeneratorOption
	{
		[FieldLoader.Require]
		public readonly string Parameter = null;
		public readonly bool Default = false;
		public bool Value;

		public MapGeneratorBooleanOption(string id, MiniYaml yaml)
			: base(id, yaml)
		{
			Value = Default;
		}

		public override IReadOnlyCollection<MiniYamlNode> GetSettings(ITerrainInfo terrainInfo, int playerCount)
		{
			return [new MiniYamlNode(Parameter, FieldSaver.FormatValue(Value))];
		}
	}

	public class MapGeneratorIntegerOption : MapGeneratorOption
	{
		[FieldLoader.Require]
		public readonly string Parameter = null;
		public readonly int Default = 0;
		public int Value;

		public MapGeneratorIntegerOption(string id, MiniYaml yaml)
			: base(id, yaml)
		{
			Value = Default;
		}

		public override IReadOnlyCollection<MiniYamlNode> GetSettings(ITerrainInfo terrainInfo, int playerCount)
		{
			return [new MiniYamlNode(Parameter, FieldSaver.FormatValue(Value))];
		}
	}

	public class MapGeneratorMultiChoiceOption : MapGeneratorOption
	{
		public class MapGeneratorDropdownChoice
		{
			public readonly string Label = null;
			public readonly string[] Tileset = null;
			public readonly int[] Players = null;

			[FieldLoader.LoadUsing(nameof(LoadSettings))]
			[FieldLoader.Require]
			public readonly ImmutableList<MiniYamlNode> Settings = null;

			static ImmutableList<MiniYamlNode> LoadSettings(MiniYaml yaml)
			{
				return yaml.NodeWithKey("Settings").Value.Nodes.ToImmutableList();
			}
		}

		[FieldLoader.LoadUsing(nameof(LoadChoices))]
		public readonly Dictionary<string, MapGeneratorDropdownChoice> Choices = null;

		static Dictionary<string, MapGeneratorDropdownChoice> LoadChoices(MiniYaml yaml)
		{
			var ret = new Dictionary<string, MapGeneratorDropdownChoice>();
			foreach (var node in yaml.Nodes)
			{
				var split = node.Key.Split('@');
				if (split.Length == 2 && split[0] == "Choice")
					ret.Add(split[1], FieldLoader.Load<MapGeneratorDropdownChoice>(node.Value));
			}

			return ret;
		}

		public readonly string[] Default = null;
		string value = null;

		public MapGeneratorMultiChoiceOption(string id, MiniYaml yaml)
			: base(id, yaml) { }

		public override IReadOnlyCollection<MiniYamlNode> GetSettings(ITerrainInfo terrainInfo, int playerCount)
		{
			var validChoices = ValidChoices(terrainInfo, playerCount);
			if (validChoices.Contains(value))
				return Choices[value].Settings;

			var fallback = Default?.FirstOrDefault(validChoices.Contains) ?? validChoices.FirstOrDefault();
			return fallback != null ? Choices[fallback].Settings : [];
		}

		public string Value
		{
			get => value;
			set
			{
				if (value != null && !Choices.ContainsKey(value))
					throw new ArgumentException($"{value} is not in the list of valid choices");

				this.value = value;
			}
		}

		public List<string> ValidChoices(ITerrainInfo terrainInfo, int playerCount)
		{
			return Choices
				.Where(kv =>
					(kv.Value.Tileset?.Contains(terrainInfo.Id) ?? true) &&
					(kv.Value.Players?.Contains(playerCount) ?? true))
				.Select(kv => kv.Key)
				.ToList();
		}

		public override IEnumerable<string> GetFluentReferences()
		{
			if (Label != null)
				yield return Label;

			foreach (var c in Choices.Values)
			{
				if (c.Label == null)
					continue;

				yield return c.Label + ".label";

				// Descriptions are optional
				if (FluentProvider.TryGetMessage(c.Label + ".description", out _))
					yield return c.Label + ".description";
			}
		}
	}

	public class MapGeneratorMultiIntegerChoiceOption : MapGeneratorOption
	{
		[FieldLoader.Require]
		public readonly string Parameter = null;

		[FieldLoader.Require]
		public readonly int[] Choices = null;

		public readonly int? Default = null;
		int value;

		public MapGeneratorMultiIntegerChoiceOption(string id, MiniYaml yaml)
			: base(id, yaml)
		{
			Value = Default ?? Choices?.First() ?? 0;
		}

		public int Value
		{
			get => value;
			set
			{
				if (!Choices.Contains(value))
					throw new ArgumentException($"{value} is not in the list of valid choices");

				this.value = value;
			}
		}

		public override IReadOnlyCollection<MiniYamlNode> GetSettings(ITerrainInfo terrainInfo, int playerCount)
		{
			return [new MiniYamlNode(Parameter, FieldSaver.FormatValue(Value))];
		}
	}

	public sealed class MapGeneratorSettings : IMapGeneratorSettings
	{
		sealed class MapGenerationArgsWithOptions : MapGenerationArgs
		{
			public Dictionary<string, string> Options = [];
		}

		readonly IMapGeneratorInfo generatorInfo;

		public MapGeneratorSettings(IMapGeneratorInfo generatorInfo, MiniYaml yaml)
		{
			this.generatorInfo = generatorInfo;
			foreach (var node in yaml.Nodes)
			{
				var split = node.Key.Split('@');
				if (split.Length != 2)
					continue;

				if (split[0] == "BooleanOption")
					Options.Add(new MapGeneratorBooleanOption(split[1], node.Value));
				else if (split[0] == "IntegerOption")
					Options.Add(new MapGeneratorIntegerOption(split[1], node.Value));
				else if (split[0] == "MultiIntegerChoiceOption")
					Options.Add(new MapGeneratorMultiIntegerChoiceOption(split[1], node.Value));
				else if (split[0] == "MultiChoiceOption")
					Options.Add(new MapGeneratorMultiChoiceOption(split[1], node.Value));
			}
		}

		public List<MapGeneratorOption> Options { get; } = [];

		public void Randomize(MersenneTwister random)
		{
			if (Options.FirstOrDefault(o => o.Id == "Seed") is MapGeneratorIntegerOption seed)
				seed.Value = random.Next();
		}

		public int PlayerCount
		{
			get
			{
				var o = Options.FirstOrDefault(o => o.Id == "Players");
				if (o is MapGeneratorIntegerOption io)
					return io.Value;

				if (o is MapGeneratorMultiIntegerChoiceOption mio)
					return mio.Value;

				return 0;
			}
		}

		public void Initialize(MapGenerationArgs args)
		{
			if (args is MapGenerationArgsWithOptions optionArgs)
			{
				foreach (var o in Options)
				{
					if (!optionArgs.Options.TryGetValue(o.Id, out var value))
						continue;

					switch (o)
					{
						case MapGeneratorBooleanOption bo: bo.Value = FieldLoader.GetValue<bool>(o.Id, value); break;
						case MapGeneratorIntegerOption io: io.Value = FieldLoader.GetValue<int>(o.Id, value); break;
						case MapGeneratorMultiIntegerChoiceOption mio: mio.Value = FieldLoader.GetValue<int>(o.Id, value); break;
						case MapGeneratorMultiChoiceOption mo: mo.Value = value; break;
					}
				}
			}
		}

		public MapGenerationArgs Compile(ITerrainInfo terrainInfo, Size size)
		{
			// Apply the choices in their canonical order.
			var playerCount = PlayerCount;
			var layers = Options
				.OrderBy(option => option.Priority)
				.Select(o => o.GetSettings(terrainInfo, playerCount));

			var options = new Dictionary<string, string>();
			foreach (var o in Options)
			{
				switch (o)
				{
					case MapGeneratorBooleanOption bo: options[o.Id] = FieldSaver.FormatValue(bo.Value); break;
					case MapGeneratorIntegerOption io: options[o.Id] = FieldSaver.FormatValue(io.Value); break;
					case MapGeneratorMultiIntegerChoiceOption mio: options[o.Id] = FieldSaver.FormatValue(mio.Value); break;
					case MapGeneratorMultiChoiceOption mo: options[o.Id] = mo.Value; break;
				}
			}

			return new MapGenerationArgsWithOptions()
			{
				Generator = generatorInfo.Type,
				Tileset = terrainInfo.Id,
				Size = size,
				Title = FluentProvider.GetMessage(generatorInfo.MapTitle),
				Author = FluentProvider.GetMessage(generatorInfo.Name),
				Settings = new MiniYaml(null, MiniYaml.Merge(layers)),
				Options = options
			};
		}
	}
}
