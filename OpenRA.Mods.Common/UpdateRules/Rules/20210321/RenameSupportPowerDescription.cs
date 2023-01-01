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
	public class RenameSupportPowerDescription : UpdateRule
	{
		public override string Name => "Support powers now use 'Name' and 'Description' fields like units.";
		public override string Description => "'Description' was renamed to 'Name' and 'LongDesc' was renamed to 'Description'.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var traitNode in actorNode.ChildrenContaining("Power"))
			{
				traitNode.RenameChildrenMatching("Description", "Name");
				traitNode.RenameChildrenMatching("LongDesc", "Description");
			}

			yield break;
		}
	}
}
