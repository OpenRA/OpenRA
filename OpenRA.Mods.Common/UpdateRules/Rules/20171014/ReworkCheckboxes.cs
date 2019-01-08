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
	public class ReworkCheckboxes : UpdateRule
	{
		public override string Name { get { return "Rename 'Locked' and 'Enabled' on checkboxes and dropdowns"; } }
		public override string Description
		{
			get
			{
				return "'Locked' and 'Enabled' were renamed to contain the respective checkboxes' name,\n" +
					"like 'FogCheckboxLocked'. For dropdowns 'Locked' was renamed to 'DropdownLocked'.";
			}
		}

		public override IEnumerable<string> UpdateActorNode(ModData modData, MiniYamlNode actorNode)
		{
			var mpUnits = actorNode.LastChildMatching("SpawnMPUnits");
			if (mpUnits != null)
			{
				var locked = mpUnits.LastChildMatching("Locked");
				if (locked != null)
					locked.RenameKey("DropdownLocked");
			}

			var shroud = actorNode.LastChildMatching("Shroud");
			if (shroud != null)
			{
				var fogLocked = shroud.LastChildMatching("FogLocked");
				if (fogLocked != null)
					fogLocked.RenameKey("FogCheckboxLocked");

				var fogEnabled = shroud.LastChildMatching("FogEnabled");
				if (fogEnabled != null)
					fogEnabled.RenameKey("FogCheckboxEnabled");

				var exploredMapLocked = shroud.LastChildMatching("ExploredMapLocked");
				if (exploredMapLocked != null)
					exploredMapLocked.RenameKey("ExploredMapCheckboxLocked");

				var exploredMapEnabled = shroud.LastChildMatching("ExploredMapEnabled");
				if (exploredMapEnabled != null)
					exploredMapEnabled.RenameKey("ExploredMapCheckboxEnabled");
			}

			var options = actorNode.LastChildMatching("MapOptions");
			if (options != null)
			{
				var shortGameLocked = options.LastChildMatching("ShortGameLocked");
				if (shortGameLocked != null)
					shortGameLocked.RenameKey("ShortGameCheckboxLocked");

				var shortGameEnabled = options.LastChildMatching("ShortGameEnabled");
				if (shortGameEnabled != null)
					shortGameEnabled.RenameKey("ShortGameCheckboxEnabled");

				var techLevelLocked = options.LastChildMatching("TechLevelLocked");
				if (techLevelLocked != null)
					techLevelLocked.RenameKey("TechLevelDropdownLocked");

				var gameSpeedLocked = options.LastChildMatching("GameSpeedLocked");
				if (gameSpeedLocked != null)
					gameSpeedLocked.RenameKey("GameSpeedDropdownLocked");
			}

			var creeps = actorNode.LastChildMatching("MapCreeps");
			if (creeps != null)
			{
				var locked = creeps.LastChildMatching("Locked");
				if (locked != null)
					locked.RenameKey("CheckboxLocked");

				var enabled = creeps.LastChildMatching("Enabled");
				if (enabled != null)
					enabled.RenameKey("CheckboxEnabled");
			}

			var buildRadius = actorNode.LastChildMatching("MapBuildRadius");
			if (buildRadius != null)
			{
				var alllyLocked = buildRadius.LastChildMatching("AllyBuildRadiusLocked");
				if (alllyLocked != null)
					alllyLocked.RenameKey("AllyBuildRadiusCheckboxLocked");

				var allyEnabled = buildRadius.LastChildMatching("AllyBuildRadiusEnabled");
				if (allyEnabled != null)
					allyEnabled.RenameKey("AllyBuildRadiusCheckboxEnabled");

				var buildRadiusLocked = buildRadius.LastChildMatching("BuildRadiusLocked");
				if (buildRadiusLocked != null)
					buildRadiusLocked.RenameKey("BuildRadiusCheckboxLocked");

				var buildRadiusEnabled = buildRadius.LastChildMatching("BuildRadiusEnabled");
				if (buildRadiusEnabled != null)
					buildRadiusEnabled.RenameKey("BuildRadiusCheckboxEnabled");
			}

			var devMode = actorNode.LastChildMatching("DeveloperMode");
			if (devMode != null)
			{
				var locked = devMode.LastChildMatching("Locked");
				if (locked != null)
					locked.RenameKey("CheckboxLocked");

				var enabled = devMode.LastChildMatching("Enabled");
				if (enabled != null)
					enabled.RenameKey("CheckboxEnabled");
			}

			var spawner = actorNode.LastChildMatching("CrateSpawner");
			if (spawner != null)
			{
				var locked = spawner.LastChildMatching("Locked");
				if (locked != null)
					locked.RenameKey("CheckboxLocked");

				var enabled = spawner.LastChildMatching("Enabled");
				if (enabled != null)
					enabled.RenameKey("CheckboxEnabled");
			}

			var resources = actorNode.LastChildMatching("PlayerResources");
			if (resources != null)
			{
				var locked = resources.LastChildMatching("Locked");
				if (locked != null)
					locked.RenameKey("DefaultCashDropdownLocked");
			}

			yield break;
		}
	}
}
