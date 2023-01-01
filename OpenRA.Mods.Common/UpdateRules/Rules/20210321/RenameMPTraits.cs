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
	public class RenameMPTraits : UpdateRule
	{
		public override string Name => "Several traits spawning map actors and players have been renamed.";

		public override string Description =>
			"'SpawnMPUnits' was renamed to 'SpawnStartingUnits', 'MPStartUnits' to 'StartingUnits', 'MPStartLocations' to " +
			"'MapStartingLocations', and 'CreateMPPlayers' to 'CreateMapPlayers'.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			actorNode.RenameChildrenMatching("SpawnMPUnits", "SpawnStartingUnits");
			actorNode.RenameChildrenMatching("MPStartUnits", "StartingUnits");
			actorNode.RenameChildrenMatching("MPStartLocations", "MapStartingLocations");
			actorNode.RenameChildrenMatching("CreateMPPlayers", "CreateMapPlayers");
			yield break;
		}
	}
}
