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
using OpenRA.Mods.Common.MapGenerator;
using OpenRA.Mods.Common.Terrain;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckMultiBrushes : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			foreach (var (terrainInfoName, terrainInfo) in modData.DefaultTerrainInfo)
			{
				if (terrainInfo is ITemplatedTerrainInfo templatedTerrainInfo && templatedTerrainInfo.MultiBrushCollections.Count > 0)
				{
					var map = new Map(modData, terrainInfo, 1, 1);
					foreach (var (collectionName, collection) in templatedTerrainInfo.MultiBrushCollections)
						foreach (var info in collection)
						{
							try
							{
								// Includes validation of actor types and template IDs.
								var multiBrush = new MultiBrush(map, info);

								foreach (var (_, tile) in multiBrush.Tiles)
									if (!templatedTerrainInfo.TryGetTerrainInfo(tile, out var _))
										emitError($"Tileset {terrainInfoName} has invalid MultiBrush collection `{collectionName}`: tileset does not have tile {tile.Type},{tile.Index}");
							}
							catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
							{
								emitError($"Tileset {terrainInfoName} has invalid MultiBrush collection `{collectionName}`: {e.Message}");
							}
						}
				}
			}
		}
	}
}
