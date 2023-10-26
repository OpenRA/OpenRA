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
	public class RemoveConyardChronoReturnAnimation : UpdateRule
	{
		public override string Name => "Remove Sequence and Body properties from ConyardChronoReturn.";

		public override string Description => "These properties have been replaced with a dynamic vortex renderable.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var trait in actorNode.ChildrenMatching("ConyardChronoReturn"))
			{
				trait.RemoveNodes("Sequence");
				trait.RemoveNodes("Body");
			}

			yield break;
		}
	}
}
