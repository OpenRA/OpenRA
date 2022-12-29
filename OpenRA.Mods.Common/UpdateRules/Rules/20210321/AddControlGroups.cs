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
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddControlGroups : UpdateRule
	{
		public override string Name => "Add new ControlGroups trait.";

		public override string Description => "A new trait ControlGroups was added, splitting logic away from Selection.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.ChildrenMatching("Selection").Any(x => !x.IsRemoval())
			    && !actorNode.ChildrenMatching("ControlGroups").Any())
				actorNode.AddNode(new MiniYamlNode("ControlGroups", ""));

			yield break;
		}
	}
}
