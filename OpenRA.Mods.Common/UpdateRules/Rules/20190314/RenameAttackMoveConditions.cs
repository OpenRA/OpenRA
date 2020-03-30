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
	public class RenameAttackMoveConditions : UpdateRule
	{
		public override string Name { get { return "Rename AttackMove *ScanConditions"; } }
		public override string Description
		{
			get
			{
				return "AttackMove's AttackMoveScanCondition and AssaultMoveScanCondition\n" +
					"now remain active while attacking, and are have been renamed to\n" +
					"AttackMoveCondition and AssaultMoveCondition to reflect this.\n";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var at in actorNode.ChildrenMatching("AttackMove"))
			{
				foreach (var node in at.ChildrenMatching("AttackMoveScanCondition"))
					node.RenameKey("AttackMoveCondition");

				foreach (var node in at.ChildrenMatching("AssaultMoveScanCondition"))
					node.RenameKey("AssaultMoveCondition");
			}

			yield break;
		}
	}
}
