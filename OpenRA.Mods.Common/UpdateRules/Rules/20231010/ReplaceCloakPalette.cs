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
	public class ReplaceCloakPalette : UpdateRule, IBeforeUpdateActors
	{
		public override string Name => "Change default Cloak style to be done by RenderCloak* traits.";

		public override string Description =>
			"Cloak now relies on RenderCloak* traits for showing the player and allies that the actor is cloaked.";

		readonly List<(string, string)> actorsWithDefault = new();

		IEnumerable<string> IBeforeUpdateActors.BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors)
		{
			foreach (var actor in resolvedActors)
			{
				foreach (var cloak in actor.ChildrenMatching("Cloak"))
					if (cloak.LastChildMatching("Palette", false) == null)
						actorsWithDefault.Add((actor.Key, cloak.Key));
			}

			yield break;
		}

		static bool CreateRenderCloakAsAlphaIfNone(MiniYamlNodeBuilder actorNode, string suffix, MiniYamlNodeBuilder cloakType)
		{
			if (actorNode.ChildrenMatching("RenderCloakAsAlpha" + suffix).Any())
				return false;

			var list = new List<MiniYamlNode>();
			if (cloakType != null)
				list.Add(new MiniYamlNode("CloakTypes", cloakType.Value.Value));
			actorNode.AddNode(new MiniYamlNodeBuilder("RenderCloakAsAlpha" + suffix, null, list));
			return true;
		}

		static bool HasPossibleMatchingAlphaPalette(MiniYamlNodeBuilder actorNode, string suffix)
		{
			var matchingAlphaPalette = actorNode.ChildrenMatching("PaletteFromPlayerPaletteWithAlpha" + suffix, false).LastOrDefault();
			if (matchingAlphaPalette == null)
				return false;

			// Assume that Cloak is the only trait that uses this PaletteFromPlayerPaletteWithAlpha and remove it.
			if (suffix.Contains("cloak", StringComparison.InvariantCultureIgnoreCase))
			{
				actorNode.RemoveNode(matchingAlphaPalette);
				return false; // No need to warn about it.
			}

			return true;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			// Remove unnecessary PaletteFromPlayerPaletteWithAlpha traits
			var allAlphaPaletes = actorNode.ChildrenMatching("PaletteFromPlayerPaletteWithAlpha").ToArray();
			var alphaPaletesToRemove = allAlphaPaletes.Where(p => p.LastChildMatching("BaseName")?.Value.Value == "cloak").ToArray();

			foreach (var cloak in actorNode.ChildrenMatching("Cloak").ToArray())
			{
				var delimeter = cloak.Key.IndexOf('@');
				var suffix = delimeter > 0 ? "@" + cloak.Key[(delimeter + 1)..] : "";
				if (cloak.Key.StartsWith('-'))
				{
					yield return "Check whether any RenderCloak* traits should be removed from " + actorNode.Key + " with " + cloak.Key[1..] + ".";
					continue;
				}

				var palette = cloak.LastChildMatching("Palette");
				if (palette != null)
					cloak.RemoveNode(palette);
				var isPlayerPalette = cloak.LastChildMatching("IsPlayerPalette");
				if (isPlayerPalette != null)
					cloak.RemoveNode(isPlayerPalette);
				var cloakType = cloak.LastChildMatching("CloakType");
				if (palette?.Value.Value == "submerged")
				{
					var list = new List<MiniYamlNode>();
					if (cloakType != null)
						list.Add(new MiniYamlNode("CloakTypes", cloakType.Value.Value));
					actorNode.AddNode(new MiniYamlNodeBuilder("RenderCloakAsColor" + suffix, null, list));
					yield return "Check that RenderCloakAsColor" + suffix + " added to " + actorNode.Key + " is correct.";
				}
				else if (actorsWithDefault.Any(pair => pair.Item1 == actorNode.Key && pair.Item2 == cloak.Key))
				{
					if (CreateRenderCloakAsAlphaIfNone(actorNode, suffix, cloakType))
						yield return "Check that RenderCloakAsAlpha" + suffix + " added to " + actorNode.Key + " is correct.";

					if (HasPossibleMatchingAlphaPalette(actorNode, suffix))
						yield return "Check whether PaletteFromPlayerPaletteWithAlpha" + suffix + " should be removed from " + actorNode.Key + ".";
				}
				else if (palette != null && !string.IsNullOrEmpty(palette.Value.Value))
				{
					var renderTrait = actorNode.ChildrenMatching("RenderCloakWithPalette" + suffix, false).LastOrDefault();
					if (renderTrait == null)
					{
						var list = new List<MiniYamlNode>();
						if (cloakType != null)
							list.Add(new MiniYamlNode("CloakTypes", cloakType.Value.Value));
						if (isPlayerPalette != null)
							list.Add(isPlayerPalette.Build());
						list.Add(palette.Build());
						actorNode.AddNode(new MiniYamlNodeBuilder("RenderCloakWithPalette" + suffix, null, list));
						yield return "Check that RenderCloakWithPalette" + suffix + " added to " + actorNode.Key + " is correct.";
					}
					else
					{
						var paletteNode = renderTrait.LastChildMatching("Palette", false);
						if (paletteNode != null)
							renderTrait.Value.Value = palette.Value.Value;
						else
							renderTrait.AddNode(new MiniYamlNodeBuilder(palette.Build()));
						yield return "Check that RenderCloakWithPalette" + suffix + " for " + actorNode.Key + " is correct.";
					}
				}
			}

			foreach (var alphaPalette in alphaPaletesToRemove)
				actorNode.RemoveNode(alphaPalette);
			if (allAlphaPaletes.Length > alphaPaletesToRemove.Length)
				yield return actorNode.Key + " may have lingering PaletteFromPlayerPaletteWithAlpha.";
		}
	}
}
