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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class RenameContrailProperties : UpdateRule
	{
		public override string Name => "Rename contrail color properties";

		public override string Description => "Rename contrail color properties `Color` to `StartColor` and `UsePlayerColor` to `StartColorUsePlayerColor` in traits and weapons to acount for added `EndColor` functionality";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var traitNode in actorNode.ChildrenMatching("Contrail"))
				traitNode.RenameChildrenMatching("Color", "StartColor");

			foreach (var traitNode in actorNode.ChildrenMatching("Contrail"))
				traitNode.RenameChildrenMatching("UsePlayerColor", "StartColorUsePlayerColor");

			yield break;
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			foreach (var traitNode in weaponNode.ChildrenMatching("Missile"))
				traitNode.RenameChildrenMatching("ContrailColor", "ContrailStartColor");

			foreach (var traitNode in weaponNode.ChildrenMatching("Missile"))
				traitNode.RenameChildrenMatching("ContrailUsePlayerColor", "ContrailStartColorUsePlayerColor");

			foreach (var traitNode in weaponNode.ChildrenMatching("Bullet"))
				traitNode.RenameChildrenMatching("ContrailColor", "ContrailStartColor");

			foreach (var traitNode in weaponNode.ChildrenMatching("Bullet"))
				traitNode.RenameChildrenMatching("ContrailUsePlayerColor", "ContrailStartColorUsePlayerColor");

			yield break;
		}
	}
}
