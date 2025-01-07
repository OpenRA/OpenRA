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

namespace OpenRA.Mods.Common.MapGenerator
{
	public static class PlayableSpace
	{
		public enum Playability
		{
			/// <summary>Area is unplayable by land/naval units.</summary>
			Unplayable = 0,

			/// <summary>
			/// Area is unplayable by land/naval units, but should count as
			/// being "within" a playable region. This usually applies to random
			/// rock or river tiles in largely passable templates.
			/// </summary>
			Partial = 1,

			/// <summary>
			/// Area is playable by either land or naval units.
			/// </summary>
			Playable = 2,
		}

		/// <summary>
		/// Additional data for a region containing playable space.
		/// The shape of a region is specified separately via a region mask.
		/// </summary>
		public sealed class Region
		{
			/// <summary>Area of playable and partially playable space.</summary>
			public int Area;

			/// <summary>Area of fully playable space.</summary>
			public int PlayableArea;

			/// <summary>Region ID.</summary>
			public int Id;
		}

		/// <summary>Sentinel indicating a position isn't assigned to a region.</summary>
		public const int NullRegion = -1;

		/// <summary>
		/// <para>
		/// Analyses a given map's tiles and ActorPlans and determines the playable space within it.
		/// </para>
		/// <para>
		/// Requires a playabilityMap which specifies whether certain tiles are considered playable
		/// or not. Actors are always considered partially playable.
		/// </para>
		/// <para>
		/// RegionMap contains the mapping of map positions to Regions. If a map position is not
		/// within a region, the value is NullRegion.
		/// </para>
		/// </summary>
		public static (Region[] Regions, CellLayer<int> RegionMap, CellLayer<Playability> Playable) FindPlayableRegions(
			Map map,
			List<ActorPlan> actorPlans,
			Dictionary<TerrainTile, Playability> playabilityMap)
		{
			var size = map.MapSize;
			var regions = new List<Region>();
			var regionMap = new CellLayer<int>(map);
			regionMap.Clear(NullRegion);
			var playable = new CellLayer<Playability>(map);
			playable.Clear(Playability.Unplayable);
			foreach (var mpos in map.AllCells.MapCoords)
				if (map.Contains(mpos))
					playable[mpos] = playabilityMap[map.Tiles[mpos]];

			foreach (var actorPlan in actorPlans)
				foreach (var cpos in actorPlan.Footprint().Keys)
					if (map.AllCells.Contains(cpos))
					{
						var mpos = cpos.ToMPos(map);
						if (playable[mpos] == Playability.Playable)
							playable[mpos] = Playability.Partial;
					}

			void Fill(Region region, CPos start)
			{
				void AddToRegion(CPos cpos, bool fullyPlayable)
				{
					var mpos = cpos.ToMPos(map);
					regionMap[mpos] = region.Id;
					region.Area++;
					if (fullyPlayable)
						region.PlayableArea++;
				}

				bool? Filler(CPos cpos, bool fullyPlayable)
				{
					var mpos = cpos.ToMPos(map);
					if (regionMap[mpos] == NullRegion)
					{
						if (fullyPlayable && playable[mpos] == Playability.Playable)
						{
							AddToRegion(cpos, true);
							return true;
						}
						else if (playable[mpos] == Playability.Partial)
						{
							AddToRegion(cpos, false);
							return false;
						}
					}

					return null;
				}

				CellLayerUtils.FloodFill(
					playable,
					new[] { (start, true) },
					Filler,
					Direction.Spread4CVec);
			}

			foreach (var mpos in map.AllCells.MapCoords)
				if (regionMap[mpos] == NullRegion && playable[mpos] == Playability.Playable)
				{
					var region = new Region()
					{
						Area = 0,
						PlayableArea = 0,
						Id = regions.Count,
					};

					regions.Add(region);
					var cpos = mpos.ToCPos(map);
					Fill(region, cpos);
				}

			return (regions.ToArray(), regionMap, playable);
		}
	}
}
