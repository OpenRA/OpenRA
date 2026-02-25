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

namespace OpenRA.Mods.Common.GeoMapGenerator
{
	/// <summary>
	/// All user-tunable parameters for the real-world map generator.
	/// </summary>
	public sealed class GeoMapOptions
	{
		/// <summary>MGRS coordinate string (e.g., "18STJ8690017000").</summary>
		public string MgrsCoordinate { get; set; } = "";

		/// <summary>Map size in cells per side (128, 256, 512).</summary>
		public int Cells { get; set; } = 128;

		/// <summary>Meters per cell (4.0, 8.0, 16.0).</summary>
		public double MetersPerCell { get; set; } = 8.0;

		/// <summary>Tileset identifier (e.g., "TEMPERAT").</summary>
		public string Tileset { get; set; } = "TEMPERAT";

		// Feature toggles
		public bool IncludeRoads { get; set; } = true;
		public bool IncludeWater { get; set; } = true;
		public bool IncludeVegetation { get; set; } = true;
		public bool IncludeBuildings { get; set; } = true;
		public bool IncludeCoastline { get; set; } = true;
		public bool InvertCoastline { get; set; }

		// Road parameters
		public double RoadWidthMeters { get; set; } = 8.0;

		// Water parameters
		public double WaterwayWidthMeters { get; set; } = 6.0;

		// Vegetation parameters
		public double VegDensity { get; set; } = 0.15;
		public int MaxVegActors { get; set; } = 4000;
		public int VegMinSpacing { get; set; } = 2;
		public int VegPatchSize { get; set; } = 32;
		public double VegPatchBoost { get; set; } = 1.5;
		public int SuppressVegNearRoads { get; set; } = 1;
		public int SuppressVegNearBuildings { get; set; } = 1;

		// Building parameters
		public double BuildingDensity { get; set; } = 1.0;
		public int MaxBuildings { get; set; } = 1200;
		public int BuildingSearchRadius { get; set; } = 2;

		// Overpass API
		public string OverpassUrl { get; set; } = "https://overpass-api.de/api/interpreter";
		public int OverpassTimeout { get; set; } = 60;

		// Map metadata
		public string Title { get; set; }
		public string Author { get; set; } = "GeoMapGenerator";
	}
}
