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
	public class SplitNukePowerMissileImage : UpdateRule
	{
		public override string Name => "NukePower now defines the image for the missile with MissileImage.";

		public override string Description =>
			"NukePower used MissileWeapon field for as the name for missile image too.\n" +
			"This function has been moved to its own MissileImage field.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var nukePowerNode in actorNode.ChildrenMatching("NukePower"))
			{
				var missileWeaponNode = nukePowerNode.ChildrenMatching("MissileWeapon").FirstOrDefault();
				if (missileWeaponNode != null)
				{
					var weapon = missileWeaponNode.NodeValue<string>();
					nukePowerNode.AddNode(new MiniYamlNode("MissileImage", weapon));
				}
			}

			yield break;
		}
	}
}
