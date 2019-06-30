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
	public class RenameCarryallDelays : UpdateRule
	{
		public override string Name { get { return "Rename Carryall and Cargo delay parameters"; } }
		public override string Description
		{
			get
			{
				return "Carryall's LoadingDelay and UnloadingDelay parameters have been renamed\n"
					+ "to BeforeLoadDelay and BeforeUnloadDelay to match new parameters on Cargo.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var carryall in actorNode.ChildrenMatching("Carryall"))
			{
				foreach (var node in carryall.ChildrenMatching("LoadingDelay"))
					node.RenameKey("BeforeLoadDelay");

				foreach (var node in carryall.ChildrenMatching("UnloadingDelay"))
					node.RenameKey("BeforeUnloadDelay");
			}

			yield break;
		}
	}
}
