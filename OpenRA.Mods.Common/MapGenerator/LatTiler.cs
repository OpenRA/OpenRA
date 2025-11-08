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
using OpenRA.Mods.Common.Terrain;
using OpenRA.Support;

namespace OpenRA.Mods.Common.MapGenerator
{
	/// <summary>
	/// Replaces tiles to create smooth visual transistions based on "Lookup Adjacent Tile" rules.
	/// </summary>
	public sealed class LatTiler
	{
		/// <summary>
		/// Defines how a tile should be replaced based on its neighboring tiles.
		/// </summary>
		public class LatRule
		{
			static readonly string[] LookupNames = [
				"____",
				"___x",
				"__x_",
				"__xx",
				"_x__",
				"_x_x",
				"_xx_",
				"_xxx",
				"x___",
				"x__x",
				"x_x_",
				"x_xx",
				"xx__",
				"xx_x",
				"xxx_",
				"xxxx",
			];

			static List<ushort> LoadUshortList(MiniYaml my, string field)
			{
				var node = my.NodeWithKeyOrDefault(field);
				if (node == null)
					return null;

				var str = node.Value.Value;
				if (str == null)
					return [];

				return FieldLoader.GetValue<List<ushort>>(field, str);
			}

			/// <summary>
			/// The tile types that this rule considers to replace (or null to match any).
			/// </summary>
			[FieldLoader.Ignore]
			public readonly ImmutableHashSet<ushort> Main = null;

			/// <summary>
			/// Required type of a neighboring tile to match as a low bit in the lookup, or null
			/// to match any not in High. One of Low or High must be non-null.
			/// </summary>
			[FieldLoader.Ignore]
			public readonly ImmutableHashSet<ushort> Low = null;

			/// <summary>
			/// Required type of a neighboring tile to match as a high bit in the lookup, or null
			/// to match any not in Low. One of Low or High must be non-null.
			/// </summary>
			[FieldLoader.Ignore]
			public readonly ImmutableHashSet<ushort> High = null;

			/// <summary>Replacement lookup table. Array index is a bitmask of U=1, R=2, D=4, L=8.</summary>
			[FieldLoader.Ignore]
			public readonly ImmutableArray<ImmutableArray<MultiBrush>> Replacements;

			public LatRule(MiniYaml my, ITemplatedTerrainInfo itti)
			{
				FieldLoader.Load(this, my);

				var autoMain = my.NodeWithKeyOrDefault("AutoMain") != null;
				List<ushort> main;
				if (autoMain)
					main = LoadUshortList(my, "AutoMain");
				else
					main = LoadUshortList(my, "Main");

				Low = LoadUshortList(my, "Low")?.ToImmutableHashSet();
				High = LoadUshortList(my, "High")?.ToImmutableHashSet();

				if (Low == null && High == null)
					throw new YamlException("both Low and High were null in LatRule");

				if (Low != null && High != null && Low.Any(High.Contains))
					throw new YamlException("Low and High have overlap in LatRule");

				// For now, just support ushort lists. Arbitrary MultiBrushes could be supported by
				// also treating the numeric nodes as MultiBrush collections.
				var replacements = new ImmutableArray<MultiBrush>[16];
				for (var i = 0; i < 16; i++)
				{
					var node = my.NodeWithKeyOrDefault(LookupNames[i]);
					if (node == null)
						continue;

					var list = FieldLoader.GetValue<List<ushort>>(LookupNames[i], node.Value.Value);

					if (autoMain)
						main.AddRange(list);

					replacements[i] =
						list
							.Select(t => new MultiBrush().WithTemplate(itti, t, CVec.Zero, 0))
							.ToImmutableArray();
					if (replacements[i].Length == 0)
						throw new YamlException($"LatRule replacement {LookupNames[i]} has no values");
				}

				Main = main?.ToImmutableHashSet();
				Replacements = replacements.ToImmutableArray();
			}

			/// <summary>
			/// Given a tile type and its neighboring tile types, determine whether this rule
			/// specifies a replacement MultiBrush and return it if so (else null).
			/// </summary>
			/// <param name="main">The central tile, for which replacement is being considered.</param>
			/// <param name="adjacents">The surrounding -Y, +X, +Y, -X tiles (in that order).</param>
			/// <param name="random">Random source for picking replacements if multiple match.</param>
			public MultiBrush OfferReplacement(ushort main, ushort[] adjacents, MersenneTwister random)
			{
				if (Main != null && !Main.Contains(main))
					return null;

				if (Low != null &&
					High != null &&
					!adjacents.All(t => Low.Contains(t) || High.Contains(t)))
				{
					return null;
				}

				bool CheckBit(ushort type) =>
					(Low != null)
						? !Low.Contains(type)
						: High.Contains(type);

				var index =
					(CheckBit(adjacents[0]) ? 1 : 0) |
					(CheckBit(adjacents[1]) ? 2 : 0) |
					(CheckBit(adjacents[2]) ? 4 : 0) |
					(CheckBit(adjacents[3]) ? 8 : 0);

				if (Replacements[index] == null)
					return null;

				return MultiBrush.PickAny(Replacements[index], random);
			}
		}

		readonly ImmutableArray<LatRule> latRules;

		public LatTiler(ImmutableArray<LatRule> latRules)
		{
			this.latRules = latRules;
		}

		public LatTiler(MiniYaml my, ITemplatedTerrainInfo itti)
		{
			var latRules = new List<LatRule>();
			foreach (var node in my.Nodes)
			{
				var parts = node.Key.Split('@');
				switch (parts[0])
				{
					case "Rule":
						latRules.Add(new LatRule(node.Value, itti));
						break;
					default:
						throw new YamlException($"Invalid LatTiler key `{node.Key}`");
				}
			}

			this.latRules = latRules.ToImmutableArray();
		}

		/// <summary>
		/// Provided a CellLayer of tiles, runs (first matching) rules against all tiles.
		/// </summary>
		/// <param name="map">Map to offer replacements for.</param>
		/// <param name="random">Optional random source for picking replacements.</param>
		/// <returns>A MultiBrush with the applicable map edits to apply.</returns>
		public MultiBrush OfferReplacements(
			Map map,
			MersenneTwister random)
		{
			var result = new MultiBrush();
			var gridType = map.Grid.Type;

			foreach (var cpos in map.Tiles.CellRegion)
			{
				var main = map.Tiles[cpos].Type;
				ushort[] adjacents = [main, main, main, main];
				if (map.Tiles.Contains(cpos + new CVec(0, -1)))
					adjacents[0] = map.Tiles[cpos + new CVec(0, -1)].Type;

				if (map.Tiles.Contains(cpos + new CVec(1, 0)))
					adjacents[1] = map.Tiles[cpos + new CVec(1, 0)].Type;

				if (map.Tiles.Contains(cpos + new CVec(0, 1)))
					adjacents[2] = map.Tiles[cpos + new CVec(0, 1)].Type;

				if (map.Tiles.Contains(cpos + new CVec(-1, 0)))
					adjacents[3] = map.Tiles[cpos + new CVec(-1, 0)].Type;

				foreach (var latRule in latRules)
				{
					var replacement = latRule.OfferReplacement(main, adjacents, random);
					if (replacement != null)
					{
						result.MergeFrom(replacement, cpos - CPos.Zero, gridType, map.Height[cpos]);
						break;
					}
				}
			}

			return result;
		}
	}
}
