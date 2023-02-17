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
	public class SplitDamagedByTerrain : UpdateRule
	{
		public override string Name => "Several properties of 'DamagedByTerrain' have been moved to the new 'D2kBuilding' trait.";

		public override string Description => "'DamageThreshold' and 'StartOnThreshold' are no longer supported and removed from 'DamagedByTerrain'.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var damaged in actorNode.ChildrenMatching("DamagedByTerrain", includeRemovals: false))
			{
				if (damaged.RemoveNodes("DamageThreshold") > 0)
					yield return $"'DamageThreshold' was removed from {actorNode.Key} ({actorNode.Location.Filename}) without replacement.\n";
				if (damaged.RemoveNodes("StartOnThreshold") > 0)
					yield return $"'StartOnThreshold' was removed from {actorNode.Key} ({actorNode.Location.Filename}) without replacement.\n";
			}
		}
	}
}
