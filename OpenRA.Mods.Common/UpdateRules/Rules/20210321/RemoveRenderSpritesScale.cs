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
	public class RemoveRenderSpritesScale : UpdateRule
	{
		public override string Name => "Remove RenderSprites.Scale.";

		public override string Description => "The Scale option was removed from RenderSprites. Scale can now be defined on individual sequence definitions.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var renderSprites in actorNode.ChildrenMatching("RenderSprites"))
				if (renderSprites.RemoveNodes("Scale") > 0)
					yield return $"The actor-level scaling has been removed from {actorNode.Key} ({actorNode.Location.Filename}).\n" +
						"You must manually define Scale on its sequences instead.";
		}
	}
}
