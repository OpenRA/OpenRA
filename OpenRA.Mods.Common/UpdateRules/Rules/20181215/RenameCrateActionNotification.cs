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
	public class RenameCrateActionNotification : UpdateRule
	{
		public override string Name { get { return "*CrateAction>Notification renamed to Sound"; } }
		public override string Description
		{
			get
			{
				return "'*CrateAction' traits now have an actual `Notification` field.\n" +
					"The new `Sound` field does what the old `Notification` did.";
			}
		}

		string[] traits =
		{
			"DuplicateUnitCrateAction",
			"ExplodeCrateAction",
			"GiveCashCrateAction",
			"GiveMcvCrateAction",
			"GiveUnitCrateAction",
			"GrantExternalConditionCrateAction",
			"HealUnitsCrateAction",
			"HideMapCrateAction",
			"LevelUpCrateAction",
			"RevelMapCrateAction",
			"SupportPowerCrateAction"
		};

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var trait in traits)
				foreach (var action in actorNode.ChildrenMatching(trait))
					action.RenameChildrenMatching("Notification", "Sound");

			yield break;
		}
	}
}
