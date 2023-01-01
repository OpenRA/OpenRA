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
	class RenameRallyPointPath : UpdateRule
	{
		public override string Name => "Renamed RallyPoint Offset to Path";

		public override string Description => "The RallyPoint Offset property has been renamed to Path and now accepts multiple (or no) values.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var rp in actorNode.ChildrenMatching("RallyPoint"))
				rp.RenameChildrenMatching("Offset", "Path");

			yield break;
		}
	}
}
