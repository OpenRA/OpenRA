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
	class RenameProneTime : UpdateRule
	{
		public override string Name => "Renamed ProneTime to Duration";

		public override string Description => "Renamed TakeCover property ProneTime to Duration.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var takeCover in actorNode.ChildrenMatching("TakeCover"))
				takeCover.RenameChildrenMatching("ProneTime", "Duration");

			yield break;
		}
	}
}
