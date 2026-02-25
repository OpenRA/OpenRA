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
using System.Threading;
using System.Threading.Tasks;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.Terrain;
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.GeoMapGenerator
{
	/// <summary>
	/// Orchestrates the full geo-map generation pipeline:
	/// MGRS parse → compute bounds → fetch OSM → rasterize → build OpenRA Map.
	/// </summary>
	public sealed class GeoMapBuilder : IDisposable
	{
		readonly OverpassClient overpassClient;

		public GeoMapBuilder()
		{
			overpassClient = new OverpassClient();
		}

		/// <summary>
		/// Generate a complete OpenRA Map from real-world geographic data.
		/// Must be called from a background thread; use Game.RunAfterTick for UI updates.
		/// </summary>
		public Map Generate(ModData modData, GeoMapOptions options,
			Action<string, int> onProgress = null, CancellationToken ct = default)
		{
			// Phase 1: Parse MGRS coordinate
			onProgress?.Invoke("Parsing coordinates...", 2);
			ct.ThrowIfCancellationRequested();

			var (lat, lon) = MgrsConverter.ToLatLon(options.MgrsCoordinate);
			var (easting, northing, zoneNumber, zoneLetter) = UtmConverter.FromLatLon(lat, lon);
			var center = new UtmCoord(easting, northing, zoneNumber, zoneLetter);

			// Phase 2: Compute bounds
			onProgress?.Invoke("Computing map bounds...", 5);
			var bounds = GeoMath.ComputeBounds(center, options.Cells, options.MetersPerCell);
			var (south, west, north, east) = GeoMath.ComputeLatLonBbox(bounds);

			// Phase 3: Fetch OSM data
			ct.ThrowIfCancellationRequested();
			var osmJson = Task.Run(
				() => overpassClient.FetchAsync(south, west, north, east,
					options.OverpassUrl, options.OverpassTimeout, onProgress, ct),
				ct).GetAwaiter().GetResult();

			onProgress?.Invoke("Parsing OSM data...", 55);
			ct.ThrowIfCancellationRequested();
			var osmData = OsmData.Parse(osmJson);
			onProgress?.Invoke($"Parsed {osmData.NodesById.Count} nodes, {osmData.WaysById.Count} ways.", 58);

			// Phase 4: Rasterize tiles
			ct.ThrowIfCancellationRequested();
			var result = TileRasterizer.Rasterize(osmData, bounds,
				options.MetersPerCell, options.Cells, options, onProgress);

			// Phase 5: Assemble OpenRA Map
			onProgress?.Invoke("Building map...", 90);
			ct.ThrowIfCancellationRequested();

			var terrainInfo = modData.DefaultTerrainInfo.Values
				.FirstOrDefault(t => t.Id.Equals(options.Tileset, StringComparison.OrdinalIgnoreCase))
				?? modData.DefaultTerrainInfo.Values.First();

			var mapSize = options.Cells;
			var maxTerrainHeight = modData.GetOrCreate<MapGrid>().MaximumTerrainHeight;
			var map = new Map(modData, terrainInfo,
				new Size(mapSize + 2, mapSize + maxTerrainHeight + 2));

			// Set bounds
			var tl = new PPos(1, 1 + maxTerrainHeight);
			var br = new PPos(mapSize, mapSize + maxTerrainHeight);
			map.SetBounds(tl, br);

			// Populate tiles from rasterized grid
			var grid = result.Grid;
			for (var i = 0; i < grid.Width; i++)
			{
				for (var j = 0; j < grid.Height; j++)
				{
					var tileType = grid.GetType(i, j);
					var tileVariant = grid.GetVariant(i, j);
					// Map cells are offset by 1 due to border
					map.Tiles[new MPos(i + 1, j + 1 + maxTerrainHeight)] =
						new TerrainTile(tileType, tileVariant);
				}
			}

			// Set title and author
			map.Title = options.Title ?? $"RealWorld {options.MgrsCoordinate}";
			map.Author = options.Author ?? "GeoMapGenerator";
			map.Visibility = MapVisibility.Lobby;
			map.Categories = ImmutableArray.Create("RealWorld");
			map.RequiresMod = "ra";

			// Set players (0 playable — user will add in editor)
			map.PlayerDefinitions = new MapPlayers(map.Rules, 0).ToMiniYaml();

			// Set actor definitions (vegetation, buildings)
			if (result.Actors.Count > 0)
			{
				var actorNodes = new List<MiniYamlNode>();
				foreach (var actor in result.Actors)
				{
					var ar = new ActorReference(actor.ActorType)
					{
						new LocationInit(new CPos(actor.X + 1, actor.Y + 1 + maxTerrainHeight)),
						new OwnerInit("Neutral"),
					};
					actorNodes.Add(new MiniYamlNode(actor.Name, ar.Save()));
				}

				map.ActorDefinitions = actorNodes.ToImmutableArray();
			}

			// Notify terrain if needed
			if (map.Rules.TerrainInfo is ITerrainInfoNotifyMapCreated notifyMapCreated)
				notifyMapCreated.MapCreated(map);

			// Set GeoTransform metadata in RuleDefinitions
			var nwLatLon = GeoMath.CellToLatLon(0, 0, bounds, options.MetersPerCell);
			var geoYaml = $"Metadata:\n"
				+ $"\tGeoTransform:\n"
				+ $"\t\tUTMZone: {zoneNumber}{zoneLetter}\n"
				+ $"\t\tMetersPerCell: {options.MetersPerCell}\n"
				+ $"\t\tRotationDeg: 0\n"
				+ $"\t\tOrigin:\n"
				+ $"\t\t\tCorner: NW\n"
				+ $"\t\t\tLat: {nwLatLon.Lat}\n"
				+ $"\t\t\tLon: {nwLatLon.Lon}\n"
				+ $"\t\t\tUTM_E: {bounds.MinE}\n"
				+ $"\t\t\tUTM_N: {bounds.MaxN}\n"
				+ $"\t\tGrid:\n"
				+ $"\t\t\tWidth: {mapSize}\n"
				+ $"\t\t\tHeight: {mapSize}\n"
				+ $"\tAttributions:\n"
				+ $"\t\t- Name: OpenStreetMap contributors\n"
				+ $"\t\t  License: ODbL 1.0\n"
				+ $"\t\t  URL: https://www.openstreetmap.org/copyright\n"
				+ $"\t\t  Source: Overpass API: {options.OverpassUrl}\n";

			// Append metadata to existing rule definitions
			if (map.RuleDefinitions != null && !string.IsNullOrEmpty(map.RuleDefinitions.Value))
				map.RuleDefinitions = new MiniYaml(map.RuleDefinitions.Value + "\n" + geoYaml);
			else
				map.RuleDefinitions = new MiniYaml(geoYaml);

			onProgress?.Invoke("Map generation complete!", 100);
			return map;
		}

		public void Dispose()
		{
			overpassClient?.Dispose();
		}
	}
}
