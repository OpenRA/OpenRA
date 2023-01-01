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

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class AttackBomberFacingTolerance : UpdateRule
	{
		public override string Name => "Adds the old default value for AttackBomber FacingTolerance.";

		public override string Description => "The tolerance for attack angle was defined twice on AttackBomber. This override has to be defined in the rules now.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var attackBomber in actorNode.ChildrenMatching("AttackBomber", includeRemovals: false))
			{
				var facingTolerance = attackBomber.LastChildMatching("FacingTolerance");
				if (facingTolerance != null)
					continue;

				var facingToleranceNode = new MiniYamlNode("FacingTolerance", FieldSaver.FormatValue(new WAngle(8)));
				attackBomber.AddNode(facingToleranceNode);
			}

			yield break;
		}
	}
}
