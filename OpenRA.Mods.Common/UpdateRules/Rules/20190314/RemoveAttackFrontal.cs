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
	public class RemoveAttackFrontal : UpdateRule
	{
		public override string Name { get { return "Merge AttackFrontal into AttackFollow"; } }
		public override string Description
		{
			get
			{
				return "The AttackFrontal trait is now deprecated and it's functionality incorporated\n" +
				"into AttackFollow. The default AttackFrontal behaviour can be recreated by setting the\n" +
				"FacingTolerance parameter to '0' and the new FireWhileMoving parameter to 'false'.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var attackNode = actorNode.LastChildMatching("AttackFrontal");
			if (attackNode != null)
			{
				attackNode.RenameKey("AttackFollow");
				var facingToleranceNode = attackNode.LastChildMatching("FacingTolerance");
				if (facingToleranceNode == null)
					attackNode.AddNode(new MiniYamlNode("FacingTolerance", "0"));

				attackNode.AddNode(new MiniYamlNode("FireWhileMoving", "false"));
			}

			yield break;
		}
	}
}
