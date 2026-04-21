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
	public class RemoveEditorSelectionLayerProperties : UpdateRule
	{
		public override string Name => "Remove defunct properties from EditorSelectionLayer.";

		public override string Description =>
			"Map editor was refactored and many of EditorSelectionLayer properties were removed.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNodeBuilder actorNode)
		{
			foreach (var editorSelectionLayer in actorNode.ChildrenMatching("EditorSelectionLayer"))
			{
				editorSelectionLayer.RemoveNodes("Palette");
				editorSelectionLayer.RemoveNodes("FootprintAlpha");
				editorSelectionLayer.RemoveNodes("Image");
				editorSelectionLayer.RemoveNodes("CopySequence");
				editorSelectionLayer.RemoveNodes("PasteSequence");
			}

			yield break;
		}
	}
}
