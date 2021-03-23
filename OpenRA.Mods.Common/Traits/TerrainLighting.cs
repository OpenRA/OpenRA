#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using OpenRA.Primitives;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Add to the world actor to apply a global lighting tint and allow actors using the TerrainLightSource to add localised lighting.")]
	public class TerrainLightingInfo : TraitInfo, ILobbyCustomRulesIgnore
	{
		public readonly float Intensity = 1;
		public readonly float HeightStep = 0;
		public readonly float RedTint = 1;
		public readonly float GreenTint = 1;
		public readonly float BlueTint = 1;

		[Desc("Size of light source partition bins (cells)")]
		public readonly int BinSize = 10;

		public override object Create(ActorInitializer init) { return new TerrainLighting(init.World, this); }
	}

	public sealed class TerrainLighting : ITerrainLighting
	{
		class LightSource
		{
			public readonly WPos Pos;
			public readonly CPos Cell;
			public readonly WDist Range;
			public readonly float Intensity;
			public readonly float3 Tint;

			public LightSource(WPos pos, CPos cell, WDist range, float intensity, in float3 tint)
			{
				Pos = pos;
				Cell = cell;
				Range = range;
				Intensity = intensity;
				Tint = tint;
			}
		}

		readonly TerrainLightingInfo info;
		readonly Map map;
		readonly Dictionary<int, LightSource> lightSources = new Dictionary<int, LightSource>();
		readonly SpatiallyPartitioned<LightSource> partitionedLightSources;
		readonly float3 globalTint;
		int nextLightSourceToken = 1;

		public event Action<MPos> CellChanged = null;

		public TerrainLighting(World world, TerrainLightingInfo info)
		{
			this.info = info;
			map = world.Map;
			globalTint = new float3(info.RedTint, info.GreenTint, info.BlueTint);

			var cellSize = map.Grid.Type == MapGridType.RectangularIsometric ? 1448 : 1024;
			partitionedLightSources = new SpatiallyPartitioned<LightSource>(
				(map.MapSize.X + 1) * cellSize,
				(map.MapSize.Y + 1) * cellSize,
				info.BinSize * cellSize);
		}

		Rectangle Bounds(LightSource source)
		{
			var c = source.Pos;
			var r = source.Range.Length;
			return new Rectangle(c.X - r, c.Y - r, 2 * r, 2 * r);
		}

		public int AddLightSource(WPos pos, WDist range, float intensity, in float3 tint)
		{
			var token = nextLightSourceToken++;
			var source = new LightSource(pos, map.CellContaining(pos), range, intensity, tint);
			var bounds = Bounds(source);
			lightSources.Add(token, source);
			partitionedLightSources.Add(source, bounds);

			if (CellChanged != null)
				foreach (var c in map.FindTilesInCircle(source.Cell, (source.Range.Length + 1023) / 1024))
					CellChanged(c.ToMPos(map));

			return token;
		}

		public void RemoveLightSource(int token)
		{
			if (!lightSources.TryGetValue(token, out var source))
				return;

			lightSources.Remove(token);
			partitionedLightSources.Remove(source);
			if (CellChanged != null)
				foreach (var c in map.FindTilesInCircle(source.Cell, (source.Range.Length + 1023) / 1024))
					CellChanged(c.ToMPos(map));
		}

		float3 ITerrainLighting.TintAt(WPos pos)
		{
			using (new PerfSample("terrain_lighting"))
			{
				var uv = map.CellContaining(pos).ToMPos(map);
				var tint = globalTint;
				if (!map.Height.Contains(uv))
					return tint;

				var intensity = info.Intensity + info.HeightStep * map.Height[uv];
				if (lightSources.Count > 0)
				{
					foreach (var source in partitionedLightSources.At(new int2(pos.X, pos.Y)))
					{
						var range = source.Range.Length;
						var distance = (source.Pos - pos).Length;
						if (distance > range)
							continue;

						var falloff = (range - distance) * 1f / range;
						intensity += falloff * source.Intensity;
						tint += falloff * source.Tint;
					}
				}

				return intensity * tint;
			}
		}
	}
}
