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
	public class ReplaceResourceValueModifiers : UpdateRule
	{
		public override string Name => "HarvesterResourceMultiplier and RefineryResourceMultiplier replaced with ResourceValueMultiplier.";

		public override string Description => "The HarvesterResourceMultiplier trait has been removed, and the RefineryResourceMultiplier trait renamed to ResourceValueMultiplier.";

		bool notified;
		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.RemoveNodes("HarvesterResourceModifier") > 0 && !notified)
			{
				notified = true;
				yield return "The HarvesterResourceMultiplier trait is no longer supported and has been removed.";
			}

			actorNode.RenameChildrenMatching("RefineryResourceMultiplier", "ResourceValueMultiplier");
		}
	}
}
