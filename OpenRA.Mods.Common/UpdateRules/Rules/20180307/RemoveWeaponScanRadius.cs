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
	public class RemoveWeaponScanRadius : UpdateRule
	{
		public override string Name { get { return "Remove Weapon ScanRadius parameters"; } }
		public override string Description
		{
			get
			{
				return "The *ScanRadius parameters have been removed from weapon projectiles and warheads.\n" +
					"These values are now automatically determined by the engine.\n" +
					"CreateEffect.ImpactActors: False has been added to replace VictimScanRadius: 0";
			}
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			foreach (var node in weaponNode.ChildrenMatching("Warhead"))
			{
				if (node.Value.Value == "CreateEffect")
				{
					var victimScanRadius = node.LastChildMatching("VictimScanRadius");
					if (victimScanRadius != null && victimScanRadius.NodeValue<int>() == 0)
						node.AddNode("ImpactActors", "false");
					node.RemoveNodes("VictimScanRadius");
				}
			}

			var projectile = weaponNode.LastChildMatching("Projectile");
			if (projectile != null)
			{
				projectile.RemoveNodes("BounceBlockerScanRadius");
				projectile.RemoveNodes("BlockerScanRadius");
				projectile.RemoveNodes("AreaVictimScanRadius");
			}

			yield break;
		}
	}
}
