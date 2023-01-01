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
	public class RenameStances : UpdateRule
	{
		public override string Name => "Renamed player 'Stances' to 'Relationships'.";

		public override string Description =>
			"'Stances' in regards to a player have been renamed to 'Relationships'.\n" +
			"The yaml values did not change.";

		readonly (string TraitName, string OldName, string NewName)[] traits =
		{
			("Disguise", "ValidStances", "ValidRelationships"),
			("Infiltrates", "ValidStances", "ValidRelationships"),
			("AcceptsDeliveredCash", "ValidStances", "ValidRelationships"),
			("AcceptsDeliveredExperience", "ValidStances", "ValidRelationships"),
			("Armament", "TargetStances", "TargetRelationships"),
			("Armament", "ForceTargetStances", "ForceTargetRelationships"),
			("AutoTargetPriority", "ValidStances", "ValidRelationships"),
			("CaptureManagerBotModule", "CapturableStances", "CapturableRelationships"),
			("Capturable", "ValidStances", "ValidRelationships"),
			("Captures", "PlayerExperienceStances", "PlayerExperienceRelationships"),
			("ProximityExternalCondition", "ValidStances", "ValidRelationships"),
			("CreatesShroud", "ValidStances", "ValidRelationships"),
			("Demolition", "TargetStances", "TargetRelationships"),
			("Demolition", "ForceTargetStances", "ForceTargetRelationships"),
			("EngineerRepair", "ValidStances", "ValidRelationships"),
			("GivesBounty", "ValidStances", "ValidRelationships"),
			("GivesExperience", "ValidStances", "ValidRelationships"),
			("JamsMissiles", "DeflectionStances", "DeflectionRelationships"),
			("FrozenUnderFog", "AlwaysVisibleStances", "AlwaysVisibleRelationships"),
			("HiddenUnderShroud", "AlwaysVisibleStances", "AlwaysVisibleRelationships"),
			("HiddenUnderFog", "AlwaysVisibleStances", "AlwaysVisibleRelationships"),
			("AppearsOnRadar", "ValidStances", "ValidRelationships"),
			("CashTricklerBar", "DisplayStances", "DisplayRelationships"),
			("SupportPowerChargeBar", "DisplayStances", "DisplayRelationships"),
			("WithAmmoPipsDecoration", "ValidStances", "ValidRelationships"),
			("WithCargoPipsDecoration", "ValidStances", "ValidRelationships"),
			("WithDecoration", "ValidStances", "ValidRelationships"),
			("WithHarvesterPipsDecoration", "ValidStances", "ValidRelationships"),
			("WithNameTagDecoration", "ValidStances", "ValidRelationships"),
			("WithResourceStoragePipsDecoration", "ValidStances", "ValidRelationships"),
			("WithTextDecoration", "ValidStances", "ValidRelationships"),
			("WithRangeCircle", "ValidStances", "ValidRelationships"),
			("RevealOnDeath", "RevealForStances", "RevealForRelationships"),
			("RevealOnFire", "RevealForStancesRelativeToTarget", "RevealForRelationships"),
			("RevealsMap", "ValidStances", "ValidRelationships"),
			("RevealsShroud", "ValidStances", "ValidRelationships"),
			("VoiceAnnouncement", "ValidStances", "ValidRelationships"),
			("GrantExternalConditionPower", "ValidStances", "ValidRelationships"),
			("NukePower", "CameraStances", "CameraRelationships"),
			("NukePower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("AttackOrderPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("ChronoshiftPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("DropPodsPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("GpsPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("GrantPrerequisiteChargeDrainPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("IonCannonPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("AirstrikePower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("GrantExternalConditionPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("ParatroopersPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("ProduceActorPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("SpawnActorPower", "DisplayTimerStances", "DisplayTimerRelationships"),
			("TooltipDescription", "ValidStances", "ValidRelationships")
		};

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			foreach (var field in traits)
				foreach (var traitNode in actorNode.ChildrenMatching(field.TraitName))
					traitNode.RenameChildrenMatching(field.OldName, field.NewName);

			yield break;
		}

		public override IEnumerable<string> UpdateWeaponNode(ModData modData, MiniYamlNode weaponNode)
		{
			foreach (var projectileNode in weaponNode.ChildrenMatching("Projectile"))
				projectileNode.RenameChildrenMatching("ValidBounceBlockerStances", "ValidBounceBlockerRelationships");

			foreach (var warheadNode in weaponNode.ChildrenMatching("Warhead"))
				warheadNode.RenameChildrenMatching("ValidStances", "ValidRelationships");

			yield break;
		}
	}
}
