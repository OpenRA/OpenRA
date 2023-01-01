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
	public class RenameCloakTypes : UpdateRule
	{
		public override string Name => "Rename 'CloakTypes' to 'DetectionTypes'";

		public override string Description => "Rename 'CloakTypes' to 'DetectionTypes' in order to make it clearer as well as make space for 'CloakType' in Cloak";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var traitNode in actorNode.ChildrenMatching("Cloak"))
				traitNode.RenameChildrenMatching("CloakTypes", "DetectionTypes");

			foreach (var traitNode in actorNode.ChildrenMatching("DetectCloaked"))
				traitNode.RenameChildrenMatching("CloakTypes", "DetectionTypes");

			yield break;
		}
	}
}
