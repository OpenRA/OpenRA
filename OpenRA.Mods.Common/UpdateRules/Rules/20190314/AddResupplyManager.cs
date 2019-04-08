#region Copyright & License Information
/*
 * Copyright 2007-2019 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AddResupplyManager : UpdateRule
	{
		public override string Name { get { return "Split ResupplyManager from Repairable(Near)"; } }
		public override string Description
		{
			get
			{
				return "Split a ResupplyManager trait from Repairable(Near) that does all order handling.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var repairable = actorNode.ChildrenMatching("Repairable", true, false);
			if (!repairable.Any())
				repairable = actorNode.ChildrenMatching("RepairableNear", true, false);

			foreach (var r in repairable)
				actorNode.AddNode("ResupplyManager", "");

			yield break;
		}
	}
}
