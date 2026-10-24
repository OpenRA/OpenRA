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

namespace OpenRA.Mods.Common.MapGenerator
{
	public abstract class MapGeneratorOption
	{
		[Flags]
		public enum VisibilityFlags
		{
			None = 0,
			Lobby = 1,
			Editor = 2,
			All = Lobby | Editor,
		}

		[FieldLoader.Ignore]
		public readonly string Id;
		public readonly string Label = null;
		public readonly int Priority = 0;
		public readonly VisibilityFlags Visibility = VisibilityFlags.All;

		protected MapGeneratorOption(string id, MiniYaml yaml)
		{
			Id = id;
			FieldLoader.Load(this, yaml);
		}

		public abstract ImmutableArray<MiniYamlNode> GetParameters(ITerrainInfo terrainInfo, string value, int playerCount);

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

		public MapGeneratorBooleanOption(string id, MiniYaml yaml)
			: base(id, yaml) { }

		public override ImmutableArray<MiniYamlNode> GetParameters(ITerrainInfo terrainInfo, string value, int playerCount)
		{
			return [new MiniYamlNode(Parameter, FieldSaver.FormatValue(value))];
		}
	}

	public class MapGeneratorIntegerOption : MapGeneratorOption
	{
		[FieldLoader.Require]
		public readonly string Parameter = null;
		public readonly int Default = 0;

		public MapGeneratorIntegerOption(string id, MiniYaml yaml)
			: base(id, yaml) { }

		public override ImmutableArray<MiniYamlNode> GetParameters(ITerrainInfo terrainInfo, string value, int playerCount)
		{
			return [new MiniYamlNode(Parameter, FieldSaver.FormatValue(value))];
		}
	}

	public class MapGeneratorMultiChoiceOption : MapGeneratorOption
	{
		public class MapGeneratorDropdownChoice
		{
			public readonly string Label = null;
			public readonly ImmutableArray<string> Tileset = default;
			public readonly ImmutableArray<int> Players = default;

			[FieldLoader.LoadUsing(nameof(LoadParameters))]
			[FieldLoader.Require]
			public readonly ImmutableArray<MiniYamlNode> Parameters = default;

			static object LoadParameters(MiniYaml yaml)
			{
				var parametersNode = yaml.NodeWithKeyOrDefault("Parameters");
				if (parametersNode == null)
					return ImmutableArray<MiniYamlNode>.Empty;

				return parametersNode.Value.Nodes.ToImmutableArray();
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

		public readonly ImmutableArray<string> Default = default;

		public MapGeneratorMultiChoiceOption(string id, MiniYaml yaml)
			: base(id, yaml) { }

		public string DefaultFor(ITerrainInfo terrainInfo, int playerCount)
		{
			var validChoices = ValidChoices(terrainInfo, playerCount);
			if (Default != null)
				foreach (var value in Default)
					if (validChoices.Contains(value))
						return value;

			return validChoices.FirstOrDefault();
		}

		public override ImmutableArray<MiniYamlNode> GetParameters(ITerrainInfo terrainInfo, string value, int playerCount)
		{
			var validChoices = ValidChoices(terrainInfo, playerCount);
			if (validChoices.Contains(value))
				return Choices[value].Parameters;

			return Choices[DefaultFor(terrainInfo, playerCount)].Parameters;
		}

		public List<string> ValidChoices(ITerrainInfo terrainInfo, int playerCount)
		{
			return Choices
				.Where(kv =>
					(kv.Value.Tileset == null || kv.Value.Tileset.Contains(terrainInfo.Id)) &&
					(kv.Value.Players == null || kv.Value.Players.Contains(playerCount)))
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
		public readonly ImmutableArray<int> Choices = default;

		public readonly int? Default;

		public MapGeneratorMultiIntegerChoiceOption(string id, MiniYaml yaml)
			: base(id, yaml)
		{
			Default ??= Choices != null ? Choices[0] : 0;
		}

		public override ImmutableArray<MiniYamlNode> GetParameters(ITerrainInfo terrainInfo, string value, int playerCount)
		{
			return [new MiniYamlNode(Parameter, FieldSaver.FormatValue(value))];
		}
	}
}
