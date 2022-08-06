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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RemoveRepairableNear : UpdateRule
	{
		public override string Name => "Merge RepairableNear into Repairable.";

		public override string Description => "RepairableNear was obsoleted. Use Repairable with CloseEnough instead.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var repairable in actorNode.ChildrenMatching("RepairableNear"))
			{
				repairable.RenameKey("Repairable");

				var close = repairable.ChildrenContaining("CloseEnough");
				if (!close.Any() && !repairable.IsRemoval())
					repairable.AddNode("CloseEnough", "4c0");
			}

			yield break;
		}
	}
}
