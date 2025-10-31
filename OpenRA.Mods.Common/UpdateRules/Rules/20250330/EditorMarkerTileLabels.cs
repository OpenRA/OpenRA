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
using OpenRA.Primitives;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	/// <summary>
	/// Adds color names to the editor history list.
	/// </summary>
	public class EditorMarkerTileLabels : UpdateRule, IBeforeUpdateActors
	{
		public override string Name => "Add labels to MarkerLayerOverlay colors.";
		public override string Description => "Adds color names to the editor history list.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var layerNode in actorNode.ChildrenMatching("MarkerLayerOverlay"))
			{
				foreach (var colorsNode in layerNode.ChildrenMatching("Colors"))
				{
					var colors = FieldLoader.GetValue<Color[]>("Colors", colorsNode.Value.Value);
					colorsNode.Value.Value = null;
					foreach (var color in colors)
					{
						var c = FieldSaver.FormatValue(color);
						colorsNode.AddNode("notification-added-marker-tiles-markers." + c, c);
					}
				}
			}

			yield break;
		}
	}
}
