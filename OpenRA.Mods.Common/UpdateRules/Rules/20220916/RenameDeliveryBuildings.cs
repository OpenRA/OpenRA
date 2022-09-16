#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
	public class RenameDeliveryBuildings : UpdateRule
	{
		public override string Name => "Change 'DeliveryBuildings' to 'DeliveryActors' in the Harvester trait definitions.";

		public override string Description =>
			"'DeliveryBuildings' is no longer valid. " +
			"Use 'DeliveryActors' instead.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var traitNode in actorNode.Value.Nodes)
			{
				if (string.Equals(traitNode.Key, "DeliveryBuildings", System.StringComparison.InvariantCultureIgnoreCase))
					traitNode.Key = "DeliveryActors";
			}

			yield break;
		}

		bool displayed;

		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			if (displayed)
				yield break;

			displayed = true;
			yield return "'DeliveryBuildings' has been renamed to 'DeliveryActors' in the mod rules. "
			             + "Chrome yaml files may need a manual update.";
		}
	}
}
