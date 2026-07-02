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

namespace OpenRA.Mods.Common.Widgets
{
	public sealed class MapPreviewLegendHighlights
	{
		public HashSet<MPos> TerrainCells { get; } = [];
		public HashSet<CPos> SpawnPoints { get; } = [];

		public bool IsEmpty => TerrainCells.Count == 0 && SpawnPoints.Count == 0;

		public void Clear()
		{
			TerrainCells.Clear();
			SpawnPoints.Clear();
		}

		public void UnionWith(MapPreviewLegendHighlights other)
		{
			TerrainCells.UnionWith(other.TerrainCells);
			SpawnPoints.UnionWith(other.SpawnPoints);
		}
	}
}
