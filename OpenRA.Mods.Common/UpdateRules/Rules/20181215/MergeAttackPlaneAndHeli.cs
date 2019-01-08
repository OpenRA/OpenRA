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
	public class MergeAttackPlaneAndHeli : UpdateRule
	{
		public override string Name { get { return "AttackPlane and AttackHeli were merged to AttackAircraft"; } }
		public override string Description
		{
			get
			{
				return "The AttackPlane and AttackHeli traits were merged intto a single\n" +
					"AttackAircraft trait.";
			}
		}

		bool displayedMessage;
		public override IEnumerable<string> AfterUpdate(ModData modData)
		{
			var message = "If an actor had a total of more than one AttackPlane and/or AttackHeli,\n"
				+ "you may want to check the update results for possible redundant entries.\n";

			if (!displayedMessage)
				yield return message;

			displayedMessage = true;
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var attackPlanes = actorNode.ChildrenMatching("AttackPlane");
			var attackHelis = actorNode.ChildrenMatching("AttackHeli");
			var attackPlanesTotal = attackPlanes.Count();
			var attackHelisTotal = attackHelis.Count();

			if (attackPlanesTotal == 0 && attackHelisTotal == 0)
				yield break;
			else if (attackPlanesTotal == 1 && attackHelisTotal == 0)
				foreach (var attackPlane in attackPlanes)
					attackPlane.RenameKey("AttackAircraft");
			else if (attackPlanesTotal == 0 && attackHelisTotal == 1)
				foreach (var attackHeli in attackHelis)
					attackHeli.RenameKey("AttackAircraft");
			else
			{
				// If we got here, we have at least two AttackPlane/-Heli traits in total
				var attackPlanesCount = 0;
				foreach (var attackPlane in attackPlanes)
				{
					var suffixCount = attackPlanesCount > 0 ? attackPlanesCount.ToString() : "";
					attackPlane.RenameKey("AttackAircraft@Plane" + suffixCount, false, true);
					++attackPlanesCount;
				}

				var attackHelisCount = 0;
				foreach (var attackHeli in attackHelis)
				{
					var suffixCount = attackHelisCount > 0 ? attackHelisCount.ToString() : "";
					attackHeli.RenameKey("AttackAircraft@Heli" + suffixCount, false, true);
					++attackHelisCount;
				}
			}

			yield break;
		}
	}
}
