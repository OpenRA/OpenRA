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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RenameEmitInfantryOnSell : UpdateRule
	{
		public override string Name { get { return "EmitInfantryOnSell renamed to SpawnActorsOnSell"; } }
		public override string Description
		{
			get
			{
				return "The EmitInfantryOnSell trait was renamed to SpawnActorsOnSell and the default\n" +
					"actor type to spawn was removed. Uses of the old traits and defaults are updated\n" +
					"to account for this.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var eios in actorNode.ChildrenMatching("EmitInfantryOnSell"))
			{
				eios.RenameKey("SpawnActorsOnSell");
				var actortypes = eios.LastChildMatching("ActorTypes");
				if (actortypes == null)
					eios.AddNode("ActorTypes", "e1");
			}

			yield break;
		}
	}
}
