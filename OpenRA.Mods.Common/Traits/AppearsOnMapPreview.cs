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

using System.Collections.Generic;
using System.Linq;
using OpenRA.Graphics;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Render this actor when creating the minimap while saving the map.")]
	public class AppearsOnMapPreviewInfo : TraitInfo<AppearsOnMapPreview>, IMapPreviewSignatureInfo, Requires<IOccupySpaceInfo>
	{
		[Desc("Use this color to render the actor, instead of owner player color.")]
		public readonly Color Color = default(Color);

		[Desc("Use this terrain color to render the actor, instead of owner player color.",
			"Overrides `Color` if both set.")]
		public readonly string Terrain = null;

		void IMapPreviewSignatureInfo.PopulateMapPreviewSignatureCells(Map map, ActorInfo ai, ActorReference s, List<Pair<MPos, Color>> destinationBuffer)
		{
			var tileSet = map.Rules.TileSet;

			Color color;
			if (!string.IsNullOrEmpty(Terrain))
			{
				color = tileSet[tileSet.GetTerrainIndex(Terrain)].Color;
			}
			else if (Color != default(Color))
			{
				color = Color;
			}
			else
			{
				var owner = map.PlayerDefinitions.Single(p => s.InitDict.Get<OwnerInit>().InternalName == p.Value.Nodes.Last(k => k.Key == "Name").Value.Value);
				var colorValue = owner.Value.Nodes.Where(n => n.Key == "Color");
				var ownerColor = colorValue.Any() ? colorValue.First().Value.Value : "FFFFFF";
				Color.TryParse(ownerColor, out color);
			}

			var ios = ai.TraitInfo<IOccupySpaceInfo>();
			var cells = ios.OccupiedCells(ai, s.InitDict.Get<LocationInit>().Value);
			foreach (var cell in cells)
				destinationBuffer.Add(new Pair<MPos, Color>(cell.Key.ToMPos(map), color));
		}
	}

	public class AppearsOnMapPreview { }
}
