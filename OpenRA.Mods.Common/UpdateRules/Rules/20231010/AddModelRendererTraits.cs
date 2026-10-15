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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddModelRendererTraits : UpdateRule, IBeforeUpdateActors
	{
		public override string Name => "Add VoxelCache and ModelRenderer to the world actor.";

		public override string Description =>
			"The ModelSequenceFormat manifest definition was replaced by the VoxelCache and ModelRenderer world traits.";

		bool parsedModRules;
		bool needsTraits;
		bool added;
		bool reported;

		IEnumerable<string> IBeforeUpdateActors.BeforeUpdateActors(ModData modData, List<MiniYamlNodeBuilder> resolvedActors)
		{
			// Only decide for the mod rules. Maps are updated before the mod rules are saved,
			// so they would otherwise still see a mod without ModelRenderer.
			if (parsedModRules)
				yield break;

			parsedModRules = true;
			needsTraits = modData.Manifest.ModelSequences.Length > 0
				&& !resolvedActors.Any(a => a.LastChildMatching("ModelRenderer", false) != null);
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			if (!needsTraits || actorNode.LastChildMatching("TerrainRenderer", false) == null)
				yield break;

			if (actorNode.LastChildMatching("VoxelCache", false) == null)
				actorNode.AddNode("VoxelCache", "");

			actorNode.AddNode("ModelRenderer", "");
			added = true;
		}

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (reported)
				yield break;

			reported = true;
			var missing = needsTraits && !added;
			needsTraits = false;

			if (missing)
				yield return "Add VoxelCache and ModelRenderer to the world actor(s) that render voxel models.";
		}
	}
}
