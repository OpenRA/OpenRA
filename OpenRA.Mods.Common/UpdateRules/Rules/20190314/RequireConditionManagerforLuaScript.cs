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

using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RequireConditionManagerForLuaScript : UpdateRule
	{
		public override string Name { get { return "Add ConditonManager to World actor when LuaScript is used."; } }
		public override string Description
		{
			get { return "The LuaScript trait is now able to use conditions and thus\n" + "requires a ConditionManager when it is used."; }
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			if (actorNode.Key != "World")
				yield break;

			if (actorNode.LastChildMatching("LuaScript", false) != null && actorNode.LastChildMatching("ConditionManager", false) == null)
				actorNode.AddNode("ConditionManager", null);
		}
	}
}
