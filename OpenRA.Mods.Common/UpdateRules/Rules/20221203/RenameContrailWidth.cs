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
	public class RenameContrailWidth : UpdateRule
	{
		public override string Name => "Rename contrail width";

		public override string Description => "Rename contrail `TrailWidth` to `StartWidth` in traits and weapons to acount for added `EndWidth` functionality";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var traitNode in actorNode.ChildrenMatching("Contrail"))
				traitNode.RenameChildrenMatching("TrailWidth", "StartWidth");

			yield break;
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			foreach (var traitNode in weaponNode.ChildrenMatching("Projectile").Where(n => n.Value.Value == "Missile"))
				traitNode.RenameChildrenMatching("ContrailWidth", "ContrailStartWidth");

			foreach (var traitNode in weaponNode.ChildrenMatching("Projectile").Where(n => n.Value.Value == "Bullet"))
				traitNode.RenameChildrenMatching("ContrailWidth", "ContrailStartWidth");

			yield break;
		}
	}
}
