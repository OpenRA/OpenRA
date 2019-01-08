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
	public class DefineLevelUpImageDefault : UpdateRule
	{
		public override string Name { get { return "Unhardcoded LevelUpImage and LevelUpSequence on GainsExperience"; } }
		public override string Description
		{
			get
			{
				return "GainsExperience was hardcoded to play a 'levelup' crate effect from 'crate-effects' image.";
			}
		}

		static readonly string[] CrateActionTraits =
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
			"RevealMapCrateAction",
			"SupportPowerCrateAction"
		};

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var levelUpImageNode = new MiniYamlNode("LevelUpImage", "crate-effects");
			foreach (var ge in actorNode.ChildrenMatching("GainsExperience"))
				ge.AddNode(levelUpImageNode);

			foreach (var t in CrateActionTraits)
			{
				foreach (var ca in actorNode.ChildrenMatching(t))
				{
					var effect = ca.LastChildMatching("Effect");
					if (effect != null)
						effect.RenameKey("Sequence");
				}
			}

			yield break;
		}
	}
}
