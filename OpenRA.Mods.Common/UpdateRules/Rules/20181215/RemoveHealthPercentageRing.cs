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
	public class RemoveHealthPercentageRing : UpdateRule
	{
		public override string Name { get { return "Remove ring support from HealthPercentageDamage warheads' Spread"; } }
		public override string Description
		{
			get
			{
				return "Setting a second value in this warheads' Spread to define a 'ring' is no longer supported.";
			}
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			foreach (var node in weaponNode.ChildrenMatching("Warhead"))
			{
				if (node.NodeValue<string>() == "HealthPercentageDamage")
				{
					foreach (var spreadNode in node.ChildrenMatching("Spread"))
					{
						var oldValue = spreadNode.NodeValue<string[]>();
						if (oldValue.Length > 1)
							spreadNode.ReplaceValue(oldValue[0]);
					}
				}
			}

			yield break;
		}
	}
}
