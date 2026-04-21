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
		string Name { get; }
		Size TileSize { get; }
		ImmutableArray<TerrainTypeInfo> TerrainTypes { get; }
		TerrainTileInfo GetTerrainInfo(TerrainTile r);
		bool TryGetTerrainInfo(TerrainTile r, out TerrainTileInfo info);
		byte GetTerrainIndex(string type);
		byte GetTerrainIndex(TerrainTile r);
		TerrainTile DefaultTerrainTile { get; }

		ImmutableArray<Color> HeightDebugColors { get; }
		IEnumerable<Color> RestrictedPlayerColors { get; }
		float MinHeightColorBrightness { get; }
		float MaxHeightColorBrightness { get; }
	}

	/// <summary>
	/// Describes expected discontinuities in height with neighboring tiles. Each tile has eight
	/// outgoing riser connections in a formation resembling a hash symbol (#). These specify the
	/// height of each neighboring cell corner relative to the template height. For example, a
	/// cliff tile might have a Height of 4 in the template, but connect to a lower tile at height
	/// 0 for some of its corners.
	/// </summary>
	public readonly struct Riser
	{
		/// <summary>
		/// Corner connection of a Riser definition.
		/// UL means "the upper (-Y) neighboring cell, leftward adjoining (-X) corner", whereas
		/// LU means "the leftward (-X) neighboring cell, upper adjoining (-Y) corner".
		/// </summary>
		public enum Connection
		{
			UL = 0,
			UR = 1,
			RU = 2,
			RD = 3,
			DR = 4,
			DL = 5,
			LD = 6,
			LU = 7,
		}

		const byte Default = byte.MaxValue;

		readonly ulong bits = ulong.MaxValue;

		/// <summary>
		/// Parses a riser definition from MiniYaml. Two formats are accepted: a long-hand and a
		/// short-hand. An example long-hand looks like "Riser: 6,6,0,0,0,0,6,6", specifying each
		/// connection height explicitly. A short-hand may instead look like "Riser: LU=6", which
		/// means set all left corners and upper corners to 6 (setting 4 connections in total),
		/// leaving the rest default/automatic.
		/// </summary>
		public Riser(MiniYaml my)
		{
			var definition = my?.Value;
			if (definition == null)
				return;

			string[] parts;

			parts = definition.Split(",");
			if (parts.Length == 8)
			{
				bits = 0;
				for (var i = 0; i < 8; i++)
				{
					if (!Exts.TryParseByteInvariant(parts[i], out var b))
						throw new YamlException($"`{definition}` is not a valid Riser definition");

					bits |= (ulong)b << (i * 8);
				}

				return;
			}

			parts = definition.Split("=");
			if (parts.Length == 2)
			{
				if (!Exts.TryParseByteInvariant(parts[1], out var b))
					throw new YamlException($"`{definition}` is not a valid Riser definition");

				bits = b * 0x0101010101010101u;

				// TODO: make stricter
				if (!parts[0].Contains('U', StringComparison.InvariantCultureIgnoreCase))
					bits |= 0x00_00_00_00_00_00_ff_ffu;

				if (!parts[0].Contains('R', StringComparison.InvariantCultureIgnoreCase))
					bits |= 0x00_00_00_00_ff_ff_00_00u;

				if (!parts[0].Contains('D', StringComparison.InvariantCultureIgnoreCase))
					bits |= 0x00_00_ff_ff_00_00_00_00u;

				if (!parts[0].Contains('L', StringComparison.InvariantCultureIgnoreCase))
					bits |= 0xff_ff_00_00_00_00_00_00u;

				return;
			}

			throw new YamlException($"`{definition}` is not a valid Riser definition");
		}

		readonly byte? this[int i]
		{
			get
			{
				if (i < 0 || i >= 8)
					throw new IndexOutOfRangeException();

				var b = (byte)((bits >> (i * 8)) & 0xff);
				return b != Default ? b : null;
			}
		}

		/// <summary>
		/// Fetch the expected height of the given connecting corner, or null if the tile's Height
		/// value should be used instead.
		/// </summary>
		public readonly byte? this[Connection c]
		{
			get => this[(int)c];
		}
	}

	public class TerrainTileInfo
	{
		[FieldLoader.Ignore]
		public readonly byte TerrainType = byte.MaxValue;
		public readonly byte Height;
		public readonly byte RampType;
		public readonly Color MinColor;
		public readonly Color MaxColor;
		[FieldLoader.LoadUsing(nameof(LoadRiser))]
		public readonly Riser Riser;

		// Needs to be defined for subclasses
		public static object LoadRiser(MiniYaml my)
		{
			return new Riser(my.NodeWithKeyOrDefault("Riser")?.Value);
		}

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
		public readonly ImmutableArray<string> AcceptsSmudgeType = [];
		public readonly Color Color;
		public readonly bool RestrictPlayerColor = false;

		public TerrainTypeInfo(MiniYaml my) { FieldLoader.Load(this, my); }
	}

	// HACK: Temporary placeholder to avoid having to change all the traits that reference this constant.
	// This can be removed after the palette references have been moved from traits to sequences.
	public static class TileSet
	{
		public const string TerrainPaletteInternalName = "terrain";
	}
}
