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

using System.Collections.Immutable;
using System.Linq;
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[TraitLocation(SystemActors.EditorWorld)]
	[Desc("A map generator that clears a map.")]
	public sealed class ClearMapGeneratorInfo : TraitInfo, IEditorMapGeneratorInfo
	{
		[FieldLoader.Require]
		[Desc("Human-readable name this generator uses.")]
		[FluentReference]
		public readonly string Name = null;

		[FieldLoader.Require]
		[Desc("Internal id for this map generator.")]
		public readonly string Type = null;

		[FieldLoader.Require]
		[Desc("Tilesets that are compatible with this map generator.")]
		public readonly ImmutableArray<string> Tilesets = default;

		[FluentReference]
		[Desc("The title to use for generated maps.")]
		public readonly string MapTitle = "label-random-map";

		[Desc("The widget tree to open when the tool is selected.")]
		public readonly string PanelWidget = "MAP_GENERATOR_TOOL_PANEL";

		// This is purely of interest to the linter.
		[FieldLoader.LoadUsing(nameof(FluentReferencesLoader))]
		[FluentReference]
		public readonly ImmutableArray<string> FluentReferences = default;

		[FieldLoader.LoadUsing(nameof(SettingsLoader))]
		public readonly MiniYaml Settings;

		string IMapGeneratorInfo.Type => Type;
		string IMapGeneratorInfo.Name => Name;
		string IMapGeneratorInfo.MapTitle => MapTitle;

		static MiniYaml SettingsLoader(MiniYaml my)
		{
			return my.NodeWithKey("Settings").Value;
		}

		static object FluentReferencesLoader(MiniYaml my)
		{
			return new MapGeneratorSettings(null, my.NodeWithKey("Settings").Value)
				.Options.SelectMany(o => o.GetFluentReferences()).ToImmutableArray();
		}

		public IMapGeneratorSettings GetSettings()
		{
			return new MapGeneratorSettings(this, Settings);
		}

		public Map Generate(ModData modData, MapGenerationArgs args)
		{
			var random = new MersenneTwister();
			var terrainInfo = modData.DefaultTerrainInfo[args.Tileset];

			if (!Exts.TryParseUshortInvariant(args.Settings.NodeWithKey("Tile").Value.Value, out var tileType))
				throw new YamlException("Illegal tile type");

			if (!terrainInfo.TryGetTerrainInfo(new TerrainTile(tileType, 0), out var _))
				throw new MapGenerationException("Illegal tile type");

			var map = new Map(modData, terrainInfo, args.Size);
			var terraformer = new Terraformer(args, map, modData, [], Symmetry.Mirror.None, 1);

			terraformer.InitMap();

			foreach (var mpos in map.AllCells.MapCoords)
				map.Tiles[mpos] = terraformer.PickTile(random, tileType);

			terraformer.BakeMap();

			return map;
		}

		public override object Create(ActorInitializer init)
		{
			return new ClearMapGenerator(this);
		}

		ImmutableArray<string> IEditorMapGeneratorInfo.Tilesets => Tilesets;
	}

	public class ClearMapGenerator : IEditorTool
	{
		public string Label { get; }
		public string PanelWidget { get; }
		public TraitInfo TraitInfo { get; }
		public bool IsEnabled => true;

		public ClearMapGenerator(ClearMapGeneratorInfo info)
		{
			Label = info.Name;
			PanelWidget = info.PanelWidget;
			TraitInfo = info;
		}
	}
}
