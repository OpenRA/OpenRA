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
	public class RemoveLaysTerrain : UpdateRule
	{
		public override string Name => "'LaysTerrain' has been removed in favor of the new 'D2kBuilding' trait.";

		public override string Description => "'LaysTerrain' was removed.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.RemoveNodes("LaysTerrain") > 0)
				yield return $"'LaysTerrain' was removed from {actorNode.Key} ({actorNode.Location.Filename}) without replacement.\n";

			yield break;
		}
	}
}
