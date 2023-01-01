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
	public class AttackFrontalFacingTolerance : UpdateRule
	{
		public override string Name => "Adds the old default value for AttackFrontal's FacingTolerance.";

		public override string Description => "The tolerance for the attack angle was defined twice on AttackFrontal. This override has to be defined in the rules now.";

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var attackFrontal in actorNode.ChildrenMatching("AttackFrontal", includeRemovals: false))
			{
				var facingTolerance = attackFrontal.LastChildMatching("FacingTolerance");
				if (facingTolerance != null)
					continue;

				var facingToleranceNode = new MiniYamlNode("FacingTolerance", FieldSaver.FormatValue(WAngle.Zero));
				attackFrontal.AddNode(facingToleranceNode);
			}

			yield break;
		}
	}
}
