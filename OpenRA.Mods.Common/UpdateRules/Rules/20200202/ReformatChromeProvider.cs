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
using System.Drawing;
using System.Linq;
using OpenRA.Graphics;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ReformatChromeProvider : UpdateRule
	{
		public override string Name => "Reformat UI image definitions.";

		public override string Description =>
			"The format of the chrome.yaml file defining image regions for the UI has\n" +
			"changed to support additional metadata fields. ";

		readonly List<string> overrideLocations = new List<string>();
		readonly List<string> panelLocations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (overrideLocations.Count > 0)
				yield return "Region-specific image overrides are no longer supported. The following definitions must be replaced:\n" +
				             UpdateUtils.FormatMessageList(overrideLocations);

			if (panelLocations.Count > 0)
				yield return "The following definitions appear to be panels, but could not be converted to the new PanelRegion format.\n" +
					"You may wish to define PanelRegion/PanelSides manually to reduce duplication:\n" +
				             UpdateUtils.FormatMessageList(panelLocations);

			overrideLocations.Clear();
			panelLocations.Clear();
		}

		readonly string[] edgeKeys =
		{
			"corner-tl", "corner-tr", "corner-bl", "corner-br",
			"border-t", "border-b", "border-l", "border-r"
		};

		bool ExtractPanelDefinition(MiniYamlNode chromeProviderNode, MiniYamlNode regionsNode)
		{
			var cNode = regionsNode.LastChildMatching("background");
			var hasCenter = cNode != null;
			var hasEdges = edgeKeys.Any(k => regionsNode.LastChildMatching(k) != null);

			// Not a panel
			if (!hasCenter && !hasEdges)
				return true;

			// Panels may define just the background
			if (hasCenter && !hasEdges)
			{
				var bgRect = cNode.NodeValue<Rectangle>();
				chromeProviderNode.AddNode("PanelRegion", new[]
				{
					bgRect.X, bgRect.Y,
					0, 0,
					bgRect.Width, bgRect.Height,
					0, 0
				});

				chromeProviderNode.AddNode("PanelSides", PanelSides.Center);
				regionsNode.RemoveNode(cNode);
				return true;
			}

			// Panels may define just the edges, or edges plus background
			var tlNode = regionsNode.LastChildMatching("corner-tl");
			if (tlNode == null)
				return false;

			var tlRect = tlNode.NodeValue<Rectangle>();

			var tNode = regionsNode.LastChildMatching("border-t");
			if (tNode == null)
				return false;

			var tRect = tNode.NodeValue<Rectangle>();
			if (tRect.Left != tlRect.Right || tRect.Top != tlRect.Top || tRect.Bottom != tlRect.Bottom)
				return false;

			var trNode = regionsNode.LastChildMatching("corner-tr");
			if (trNode == null)
				return false;

			var trRect = trNode.NodeValue<Rectangle>();
			if (trRect.Left != tRect.Right || trRect.Top != tRect.Top || trRect.Bottom != tRect.Bottom)
				return false;

			var lNode = regionsNode.LastChildMatching("border-l");
			if (lNode == null)
				return false;

			var lRect = lNode.NodeValue<Rectangle>();
			if (lRect.Left != tlRect.Left || lRect.Top != tlRect.Bottom || lRect.Right != tlRect.Right)
				return false;

			var rNode = regionsNode.LastChildMatching("border-r");
			if (rNode == null)
				return false;

			var rRect = rNode.NodeValue<Rectangle>();
			if (rRect.Left != trRect.Left || rRect.Top != trRect.Bottom || rRect.Bottom != lRect.Bottom || rRect.Right != trRect.Right)
				return false;

			var blNode = regionsNode.LastChildMatching("corner-bl");
			if (blNode == null)
				return false;

			var blRect = blNode.NodeValue<Rectangle>();
			if (blRect.Left != lRect.Left || blRect.Top != lRect.Bottom || blRect.Right != lRect.Right)
				return false;

			var bNode = regionsNode.LastChildMatching("border-b");
			if (bNode == null)
				return false;

			var bRect = bNode.NodeValue<Rectangle>();
			if (bRect.Left != blRect.Right || bRect.Top != blRect.Top || bRect.Bottom != blRect.Bottom || bRect.Right != tRect.Right)
				return false;

			var brNode = regionsNode.LastChildMatching("corner-br");
			if (brNode == null)
				return false;

			var brRect = brNode.NodeValue<Rectangle>();
			if (brRect.Left != bRect.Right || brRect.Top != bRect.Top || brRect.Bottom != bRect.Bottom || brRect.Right != rRect.Right)
				return false;

			// Background definition may be omitted
			if (hasCenter)
			{
				var bgRect = cNode.NodeValue<Rectangle>();
				if (bgRect.Left != lRect.Right || bgRect.Top != lRect.Top || bgRect.Bottom != lRect.Bottom || bgRect.Right != tRect.Right)
					return false;
			}

			// Define the short-form panel region
			chromeProviderNode.AddNode("PanelRegion", new[]
			{
				tlRect.X, tlRect.Y,
				tlRect.Width, tlRect.Height,
				trRect.Left - tlRect.Right, blRect.Top - tlRect.Bottom,
				brRect.Width, brRect.Height
			});

			if (!hasCenter)
				chromeProviderNode.AddNode("PanelSides", PanelSides.Edges);

			// Remove the now redundant regions
			regionsNode.RemoveNode(tlNode);
			regionsNode.RemoveNode(tNode);
			regionsNode.RemoveNode(trNode);
			regionsNode.RemoveNode(lNode);
			regionsNode.RemoveNode(rNode);
			regionsNode.RemoveNode(blNode);
			regionsNode.RemoveNode(bNode);
			regionsNode.RemoveNode(brNode);

			if (cNode != null)
				regionsNode.RemoveNode(cNode);

			return true;
		}

		public override IEnumerable<string> UpdateChromeProviderNode(ModData modData, MiniYamlNode chromeProviderNode)
		{
			// Migrate image rectangles
			var regionsNode = new MiniYamlNode("Regions", "");
			foreach (var n in chromeProviderNode.Value.Nodes)
			{
				if (n.Key == "Inherits")
					continue;

				// Reformat region as a list
				regionsNode.AddNode(n.Key, n.NodeValue<int[]>());

				if (n.Value.Nodes.Count > 0)
					overrideLocations.Add($"{chromeProviderNode.Key}.{n.Key} ({chromeProviderNode.Location.Filename})");
			}

			chromeProviderNode.Value.Nodes.RemoveAll(n => n.Key != "Inherits");

			// Migrate image definition
			chromeProviderNode.AddNode(new MiniYamlNode("Image", chromeProviderNode.Value.Value));
			chromeProviderNode.Value.Value = "";

			if (!ExtractPanelDefinition(chromeProviderNode, regionsNode))
				panelLocations.Add($"{chromeProviderNode.Key} ({chromeProviderNode.Location.Filename})");

			if (regionsNode.Value.Nodes.Count > 0)
				chromeProviderNode.AddNode(regionsNode);

			yield break;
		}
	}
}
