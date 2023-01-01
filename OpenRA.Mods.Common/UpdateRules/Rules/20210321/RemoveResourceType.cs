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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemoveResourceType : UpdateRule
	{
		public override string Name => "Remove ResourceType definitions.";

		public override string Description =>
			"The ResourceType trait has been removed, and resource definitions moved to the\n" +
			"ResourceLayer, EditorResourceLayer, ResourceRenderer, and PlayerResources traits.";

		MiniYaml resourceLayer;
		MiniYaml resourceRenderer;
		MiniYaml values;

		public override IEnumerable<string> BeforeUpdate(ModData modData)
		{
			resourceLayer = new MiniYaml("");
			resourceRenderer = new MiniYaml("");
			values = new MiniYaml("");
			yield break;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (resourceLayer.Nodes.Count > 0)
				yield return "Add the following definitions to your ResourceLayer and EditorResourceLayer definitions:\n\t" +
					"RecalculateResourceDensity: true\n\t" +
					resourceLayer.ToLines("ResourceTypes").JoinWith("\n\t");

			if (resourceLayer.Nodes.Count > 0)
				yield return "Add the following definitions to your ResourceRenderer definition:\n\t" +
					resourceRenderer.ToLines("ResourceTypes").JoinWith("\n\t");

			if (values.Nodes.Count > 0)
				yield return "Add the following definition to your ^BasePlayer definition:\n\t" +
					"PlayerResources:\n\t\t" +
					values.ToLines("ResourceValues").JoinWith("\n\t\t");

			if (resourceLayer.Nodes.Count > 0)
				yield return "Support for AllowUnderActors, AllowUnderBuildings, and AllowOnRamps have been removed.\n" +
					"You must define a custom ResourceLayer subclass if you want to customize the default behaviour.";
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var resourceNode in actorNode.ChildrenMatching("ResourceType"))
			{
				var typeNode = resourceNode.LastChildMatching("Type");
				if (typeNode != null)
				{
					var resourceLayerNode = new MiniYamlNode(typeNode.Value.Value, "");
					resourceLayer.Nodes.Add(resourceLayerNode);

					var resourceRendererNode = new MiniYamlNode(typeNode.Value.Value, "");
					resourceRenderer.Nodes.Add(resourceRendererNode);

					var indexNode = resourceNode.LastChildMatching("ResourceType");
					if (indexNode != null)
					{
						indexNode.RenameKey("ResourceIndex");
						resourceLayerNode.AddNode(indexNode);
					}

					var terrainTypeNode = resourceNode.LastChildMatching("TerrainType");
					if (terrainTypeNode != null)
						resourceLayerNode.AddNode(terrainTypeNode);

					var allowedTerrainNode = resourceNode.LastChildMatching("AllowedTerrainTypes");
					if (allowedTerrainNode != null)
						resourceLayerNode.AddNode(allowedTerrainNode);

					var maxDensityNode = resourceNode.LastChildMatching("MaxDensity");
					if (maxDensityNode != null)
						resourceLayerNode.AddNode(maxDensityNode);

					var valueNode = resourceNode.LastChildMatching("ValuePerUnit");
					if (valueNode != null)
						values.Nodes.Add(new MiniYamlNode(typeNode.Value.Value, valueNode.Value.Value));

					var imageNode = resourceNode.LastChildMatching("Image");
					if (imageNode != null)
						resourceRendererNode.AddNode(imageNode);

					var sequencesNode = resourceNode.LastChildMatching("Sequences");
					if (sequencesNode != null)
						resourceRendererNode.AddNode(sequencesNode);

					var paletteNode = resourceNode.LastChildMatching("Palette");
					if (paletteNode != null)
						resourceRendererNode.AddNode(paletteNode);

					var nameNode = resourceNode.LastChildMatching("Name");
					if (nameNode != null)
						resourceRendererNode.AddNode(nameNode);
				}
				else
					yield return "Unable to process definition:\n" +
						resourceNode.Value.ToLines(resourceNode.Key).JoinWith("\n") + "\n\n" +
						"This override has been removed and must be manually reimplemented if still needed.";
			}

			actorNode.RemoveNodes("ResourceType");

			foreach (var resourceRendererNode in actorNode.ChildrenMatching("ResourceRenderer"))
				resourceRendererNode.RemoveNodes("RenderTypes");

			foreach (var resourceRendererNode in actorNode.ChildrenMatching("D2kResourceRenderer"))
				resourceRendererNode.RemoveNodes("RenderTypes");
		}
	}
}
