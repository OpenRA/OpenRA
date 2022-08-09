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
	public class RemoveWithVoxelUnloadBody : UpdateRule
	{
		public override string Name => "Removed WithVoxelUnloadBody.";

		public override string Description =>
			"WithVoxelUnloadBody has been removed. Toggle 2 WithVoxelBody traits instead using a DockedCondition granted by Harvester.\n";
		readonly List<string> locations = new List<string>();

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (locations.Count > 0)
				yield return "WithVoxelUnloadBody has been removed from the following locations:\n" +
					UpdateUtils.FormatMessageList(locations) + "\n\n" +
					"You may wish to inspect and change these.";

			locations.Clear();
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.RemoveNodes("WithVoxelUnloadBody") > 0)
				locations.Add($"{actorNode.Key} ({actorNode.Location.Filename})");

			yield break;
		}
	}
}
