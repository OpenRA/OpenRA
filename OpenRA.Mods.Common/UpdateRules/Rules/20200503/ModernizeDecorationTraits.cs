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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ModernizeDecorationTraits : UpdateRule
	{
		public override string Name => "Modernize SelectionDecorations and With*Decoration traits.";

		public override string Description =>
			"The configuration properties exposed on the SelectionDecorations and With*Decoration\n" +
			"traits have been reworked. RenderSelectionBars and RenderSelectionBox have been removed from\n" +
			"SelectionDecorations. The obsolete ZOffset and ScreenOffset has been removed from With*Decoration, and ReferencePoint has\n" +
			"been replaced by Position which takes a single value (TopLeft, TopRight, BottomLeft, BottomRight, Center, or Top).\n" +
			"A new Margin property is available to control the decoration offset relative to the edges of the selection box.\n" +
			"RenderNameTag has been renamed to WithNameTagDecoration and now behaves like a normal decoration trait.\n";

		static readonly string[] LegacyDecorationTraits = { "WithDecoration", "WithSpriteControlGroupDecoration", "WithTextControlGroupDecoration", "WithTextDecoration", "WithBuildingRepairDecoration", "InfiltrateForDecoration" };
		static readonly string[] ModernDecorationTraits = { "WithAmmoPipsDecoration", "WithCargoPipsDecoration", "WithHarvesterPipsDecoration", "WithResourceStoragePipsDecoration", "WithNameTagDecoration" };

		[Flags]
		public enum LegacyReferencePoints
		{
			Center = 0,
			Top = 1,
			Bottom = 2,
			Left = 4,
			Right = 8,
		}

		static readonly Dictionary<LegacyReferencePoints, string> PositionMap = new Dictionary<LegacyReferencePoints, string>()
		{
			{ LegacyReferencePoints.Center, "Center" },
			{ LegacyReferencePoints.Top, "Top" },
			{ LegacyReferencePoints.Top | LegacyReferencePoints.Left, "TopLeft" },
			{ LegacyReferencePoints.Top | LegacyReferencePoints.Right, "TopRight" },
			{ LegacyReferencePoints.Bottom | LegacyReferencePoints.Left, "BottomLeft" },
			{ LegacyReferencePoints.Bottom | LegacyReferencePoints.Right, "BottomRight" }
		};

		readonly Dictionary<string, List<string>> locations = new Dictionary<string, List<string>>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "The way that decorations are positioned relative to the selection box has changed.\n" +
					"Review the following definitions and define Margin properties as required:\n" +
					UpdateUtils.FormatMessageList(locations.Select(
						kv => kv.Key + ":\n" + UpdateUtils.FormatMessageList(kv.Value)));

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var locationKey = $"{actorNode.Key} ({actorNode.Location.Filename})";

			foreach (var trait in LegacyDecorationTraits)
			{
				foreach (var node in actorNode.ChildrenMatching(trait))
				{
					node.RemoveNodes("ZOffset");
					node.RemoveNodes("ScreenOffset");

					var positionNode = node.LastChildMatching("ReferencePoint");
					if (positionNode != null)
					{
						if (!PositionMap.TryGetValue(positionNode.NodeValue<LegacyReferencePoints>(), out var value))
							value = "TopLeft";

						if (value != "TopLeft")
						{
							positionNode.RenameKey("Position");
							positionNode.ReplaceValue(FieldSaver.FormatValue(value));
						}
						else
							node.RemoveNode(positionNode);
					}

					locations.GetOrAdd(locationKey).Add(node.Key);
				}
			}

			foreach (var trait in ModernDecorationTraits)
				foreach (var node in actorNode.ChildrenMatching(trait))
					locations.GetOrAdd(locationKey).Add(node.Key);

			foreach (var selection in actorNode.ChildrenMatching("SelectionDecorations"))
			{
				selection.RemoveNodes("RenderSelectionBars");
				selection.RemoveNodes("RenderSelectionBox");
			}

			foreach (var nameTag in actorNode.ChildrenMatching("RenderNameTag"))
			{
				nameTag.RenameKey("WithNameTagDecoration");
				nameTag.AddNode("Position", "Top");
				nameTag.AddNode("UsePlayerColor", "true");
				locations.GetOrAdd(locationKey).Add(nameTag.Key);
			}

			yield break;
		}
	}
}
