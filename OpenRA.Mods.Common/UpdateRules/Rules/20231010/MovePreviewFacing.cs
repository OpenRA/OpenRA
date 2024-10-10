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
	public class MovePreviewFacing : UpdateRule
	{
		public override string Name => "Move map editor preview facing to EditorActorLayer";

		public override string Description =>
			"PreviewFacing property was moved from the EditorCursorLayer to the EditorActorLayer.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			var cursorLayer = actorNode.LastChildMatching("EditorCursorLayer");
			if (cursorLayer == null || cursorLayer.IsRemoval())
				yield break;

			var node = cursorLayer.LastChildMatching("PreviewFacing");
			cursorLayer.RemoveNodes("PreviewFacing");
			if (node == null || node.IsRemoval())
				yield break;

			var actorLayer = actorNode.LastChildMatching("EditorActorLayer");
			if (actorLayer != null && !actorLayer.IsRemoval())
			{
				node.RenameKey("DefaultActorFacing");
				actorLayer.AddNode(node);
			}
		}
	}
}
