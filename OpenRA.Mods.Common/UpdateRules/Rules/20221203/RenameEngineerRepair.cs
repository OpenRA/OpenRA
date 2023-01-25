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
	public class RenameEngineerRepair : UpdateRule
	{
		public override string Name => "Traits revolving around instant (building) repairs were renamed.";

		public override string Description =>
			"'EngineerRepair' was renamed to 'InstantlyRepairs' " +
			"and 'EngineerRepairable' to 'InstantlyRepairable'.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			actorNode.RenameChildrenMatching("EngineerRepair", "InstantlyRepairs");
			actorNode.RenameChildrenMatching("EngineerRepairable", "InstantlyRepairable");
			yield break;
		}
	}
}
