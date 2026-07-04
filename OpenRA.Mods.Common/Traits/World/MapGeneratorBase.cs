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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	public abstract class MapGeneratorBaseInfo : TraitInfo, IEditorMapGeneratorInfo
	{
		[FieldLoader.Require]
		public readonly string Type = null;

		[FieldLoader.Require]
		[FluentReference]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Tilesets that are compatible with this map generator.")]
		public readonly ImmutableArray<string> Tilesets = default;

		[FluentReference]
		[Desc("The title to use for generated maps.")]
		public readonly string MapTitle = "label-random-map";

		[Desc("The widget tree to open when the tool is selected.")]
		public readonly string PanelWidget = "MAP_GENERATOR_TOOL_PANEL";

		// This is purely of interest to the linter.
		[FieldLoader.LoadUsing(nameof(LoadFluentReferences))]
		[FluentReference]
		public readonly ImmutableArray<string> FluentReferences = default;

		[FieldLoader.LoadUsing(nameof(LoadOptions))]
		public readonly ImmutableArray<MapGeneratorOption> Options = default;

		string IMapGeneratorInfo.Type => Type;
		string IMapGeneratorInfo.Name => Name;
		string IMapGeneratorInfo.MapTitle => MapTitle;

		ImmutableArray<string> IEditorMapGeneratorInfo.Tilesets => Tilesets;
		ImmutableArray<MapGeneratorOption> IEditorMapGeneratorInfo.Options => Options;
		int IEditorMapGeneratorInfo.GetPlayerCount(MapGenerationArgs args) => GetPlayerCount(args);

		public static object LoadOptions(MiniYaml my)
		{
			var optionsNode = my.NodeWithKeyOrDefault("Options");
			if (optionsNode == null)
				return ImmutableArray<MapGeneratorOption>.Empty;

			var options = new List<MapGeneratorOption>();
			foreach (var node in optionsNode.Value.Nodes)
			{
				var split = node.Key.Split('@');
				if (split.Length != 2)
					continue;

				if (split[0] == "BooleanOption")
					options.Add(new MapGeneratorBooleanOption(split[1], node.Value));
				else if (split[0] == "IntegerOption")
					options.Add(new MapGeneratorIntegerOption(split[1], node.Value));
				else if (split[0] == "MultiIntegerChoiceOption")
					options.Add(new MapGeneratorMultiIntegerChoiceOption(split[1], node.Value));
				else if (split[0] == "MultiChoiceOption")
					options.Add(new MapGeneratorMultiChoiceOption(split[1], node.Value));
			}

			return options.ToImmutableArray();
		}

		public static object LoadFluentReferences(MiniYaml my)
		{
			return ((ImmutableArray<MapGeneratorOption>)LoadOptions(my))
				.SelectMany(o => o.GetFluentReferences())
				.ToImmutableArray();
		}

		protected MiniYaml GenerateParameterYaml(ModData modData, MapGenerationArgs args)
		{
			var terrainInfo = modData.DefaultTerrainInfo[args.Tileset];
			var playerCount = GetPlayerCount(args);

			// Apply the choices in their canonical order.
			var parameters = new Dictionary<string, MiniYaml>();
			foreach (var o in Options.OrderBy(option => option.Priority))
			{
				if (!args.Options.TryGetValue(o.Id, out var value))
					continue;

				foreach (var pn in o.GetParameters(terrainInfo, value, playerCount))
					parameters[pn.Key] = pn.Value;
			}

			return new MiniYaml(null, parameters.Select(kv => new MiniYamlNode(kv.Key, kv.Value)));
		}

		protected virtual int GetPlayerCount(MapGenerationArgs args)
		{
			if (args.Options.TryGetValue("Players", out var players))
				return FieldLoader.GetValue<int>("Players", players);

			return 0;
		}

		public abstract Map Generate(ModData modData, MapGenerationArgs args);

		public bool TryGenerateMetadata(ModData modData, MapGenerationArgs args, out MapPlayers players, out Dictionary<string, MiniYaml> ruleDefinitions)
		{
			try
			{
				// Generated maps use the default ruleset
				ruleDefinitions = [];
				players = new MapPlayers(modData.DefaultRules, GetPlayerCount(args));

				return true;
			}
			catch
			{
				players = null;
				ruleDefinitions = null;
				return false;
			}
		}
	}

	public class MapGeneratorBase : IEditorTool
	{
		public string Label { get; }
		public string PanelWidget { get; }
		public TraitInfo TraitInfo { get; }
		public bool IsEnabled { get; }

		public MapGeneratorBase(ActorInitializer init, MapGeneratorBaseInfo info)
		{
			Label = info.Name;
			PanelWidget = info.PanelWidget;
			TraitInfo = info;
			IsEnabled = info.Tilesets.Contains(init.Self.World.Map.Tileset);
		}
	}
}
