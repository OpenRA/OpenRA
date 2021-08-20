#region Copyright & License Information
/*
 * Copyright 2007-2021 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.FileSystem;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA
{
	public interface ITerrainLoader
	{
		ITerrainInfo ParseTerrain(IReadOnlyFileSystem fileSystem, string path);
	}

	public interface ITerrainInfo
	{
		string Id { get; }
		TerrainTypeInfo[] TerrainTypes { get; }
		TerrainTileInfo GetTerrainInfo(TerrainTile r);
		bool TryGetTerrainInfo(TerrainTile r, out TerrainTileInfo info);
		byte GetTerrainIndex(string type);
		byte GetTerrainIndex(TerrainTile r);
		TerrainTile DefaultTerrainTile { get; }

		Color[] HeightDebugColors { get; }
		IEnumerable<Color> RestrictedPlayerColors { get; }
		float MinHeightColorBrightness { get; }
		float MaxHeightColorBrightness { get; }
	}

	public class TerrainTileInfo
	{
		[FieldLoader.Ignore]
		public readonly byte TerrainType = byte.MaxValue;
		public readonly byte Height;
		public readonly byte RampType;
		public readonly Color MinColor;
		public readonly Color MaxColor;

		public Color GetColor(MersenneTwister random)
		{
			if (MinColor != MaxColor)
				return Exts.ColorLerp(random.NextFloat(), MinColor, MaxColor);

			return MinColor;
		}
	}

	public class TerrainTypeInfo
	{
		public readonly string Type;
		public readonly BitSet<TargetableType> TargetTypes;
		public readonly HashSet<string> AcceptsSmudgeType = new HashSet<string>();
		public readonly Color Color;
		public readonly bool RestrictPlayerColor = false;

		public TerrainTypeInfo(MiniYaml my) { FieldLoader.Load(this, my); }
	}

	// HACK: Temporary placeholder to avoid having to change all the traits that reference this constant.
	// This can be removed after the palette references have been moved from traits to sequences.
	public class TileSet
	{
		public const string TerrainPaletteInternalName = "terrain";
	}
}
