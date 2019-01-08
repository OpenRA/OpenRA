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
	public class RemoveOutOfAmmo : UpdateRule
	{
		public override string Name { get { return "Replace 'Armament.OutOfAmmo' by pausing on condition"; } }
		public override string Description
		{
			get
			{
				return "'Armament.OutOfAmmo' has been replaced by pausing on condition\n" +
					"(which is usually provided by AmmoPool).";
			}
		}

		bool messageDisplayed;

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var reloadAmmoPool = actorNode.LastChildMatching("ReloadAmmoPool");
			var armaments = actorNode.ChildrenMatching("Armament");
			var ammoPools = actorNode.ChildrenMatching("AmmoPool");

			if (reloadAmmoPool != null || !armaments.Any() || !ammoPools.Any())
				yield break;

			foreach (var pool in ammoPools)
			{
				var nameNode = pool.LastChildMatching("Armaments");
				var name = nameNode != null ? nameNode.NodeValue<string>() : "primary, secondary";
				var anyMatchingArmament = false;
				var ammoNoAmmo = new MiniYamlNode("AmmoCondition", "ammo");
				var armNoAmmo = new MiniYamlNode("PauseOnCondition", "!ammo");

				foreach (var arma in armaments)
				{
					var armaNameNode = arma.LastChildMatching("Name");
					var armaName = armaNameNode != null ? armaNameNode.NodeValue<string>() : "primary";
					if (name.Contains(armaName))
					{
						anyMatchingArmament = true;
						arma.AddNode(armNoAmmo);
					}
				}

				if (anyMatchingArmament)
				{
					pool.AddNode(ammoNoAmmo);
					if (!messageDisplayed)
					{
						yield return "Aircraft returning to base is now triggered when all armaments are paused via condition.\n" +
							"Check if any of your actors with AmmoPools may need further changes.";

						messageDisplayed = true;
					}
				}
			}

			yield break;
		}
	}
}
