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
	public class ReplaceSequenceEmbeddedPalette : UpdateRule
	{
		public override string Name => "Replace Sequence EmbeddedPalette with HasEmbeddedPalette.";

		public override string Description => "The EmbeddedPalette sequence option was replaced with a boolean HasEmbeddedPalette.";

		public override IEnumerable<string> UpdateSequenceNode(ModData modData, MiniYamlNode sequenceNode)
		{
			foreach (var sequence in sequenceNode.Value.Nodes)
				if (sequence.RemoveNodes("EmbeddedPalette") > 0)
					sequence.AddNode("HasEmbeddedPalette", true);

			yield break;
		}
	}
}
