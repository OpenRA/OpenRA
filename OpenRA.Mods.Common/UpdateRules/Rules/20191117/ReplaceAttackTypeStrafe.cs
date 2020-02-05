#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Collections.Generic;
using OpenRA.Mods.Common.Traits;

namespace OpenRA.Mods.Common.UpdateRules.Rules
{
	public class ReplaceAttackTypeStrafe : UpdateRule
	{
		public override string Name { get { return "Replaced AttackAircraft AttackType: Strafe logic."; } }
		public override string Description
		{
			get
			{
				return "The AttackType: Strafe behaviour on AttackAircraft has been renamed to Default,\n"
					+ "and AttackTurnDelay has been removed. A new AttackType: Strafe has been added, with a\n"
					+ "new StrafeRunLength parameter, designed for use with the FirstBurstTargetOffset and\n"
					+ "FollowingBurstTargetOffset weapon parameters.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var aircraft = actorNode.LastChildMatching("Aircraft");
			if (aircraft != null)
			{
				var attackAircraft = actorNode.LastChildMatching("AttackAircraft");
				if (attackAircraft == null)
					yield break;

				var attackType = attackAircraft.LastChildMatching("AttackType");
				if (attackType != null && attackType.NodeValue<AirAttackType>() == AirAttackType.Strafe)
					attackAircraft.RemoveNode(attackType);

				attackAircraft.RemoveNodes("AttackTurnDelay");
			}
		}
	}
}
