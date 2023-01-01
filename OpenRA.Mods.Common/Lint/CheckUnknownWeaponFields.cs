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

using System;
using System.Collections.Generic;
using OpenRA.FileSystem;
using OpenRA.GameRules;
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	class CheckUnknownWeaponFields : ILintPass, ILintMapPass, ILintServerMapPass
	{
		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			foreach (var f in modData.Manifest.Weapons)
				CheckWeapons(MiniYaml.FromStream(modData.DefaultFileSystem.Open(f), f), emitError, emitWarning, modData);
		}

		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			CheckMapYaml(emitError, emitWarning, modData, map, map.WeaponDefinitions);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			CheckMapYaml(emitError, emitWarning, modData, map, map.WeaponDefinitions);
		}

		string NormalizeName(string key)
		{
			var name = key.Split('@')[0];
			if (name.StartsWith("-", StringComparison.Ordinal))
				return name.Substring(1);

			return name;
		}

		void CheckWeapons(IEnumerable<MiniYamlNode> weapons, Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			var weaponInfo = typeof(WeaponInfo);
			foreach (var weapon in weapons)
			{
				foreach (var field in weapon.Value.Nodes)
				{
					// Removals can never define children or values
					if (field.Key.StartsWith("-", StringComparison.Ordinal))
					{
						if (field.Value.Nodes.Count > 0)
							emitError($"{field.Location} {field.Key} defines child nodes, which is not valid for removals.");

						if (!string.IsNullOrEmpty(field.Value.Value))
							emitError($"{field.Location} {field.Key} defines a value, which is not valid for removals.");

						continue;
					}

					var fieldName = NormalizeName(field.Key);
					if (fieldName == "Projectile" && !string.IsNullOrEmpty(field.Value.Value))
					{
						var projectileName = NormalizeName(field.Value.Value);
						var projectileInfo = modData.ObjectCreator.FindType(projectileName + "Info");
						if (projectileInfo == null)
						{
							emitError($"{field.Location} defines unknown projectile `{projectileName}`.");
							continue;
						}

						foreach (var projectileField in field.Value.Nodes)
						{
							var projectileFieldName = NormalizeName(projectileField.Key);
							if (projectileInfo.GetField(projectileFieldName) == null)
								emitError($"{projectileField.Location} refers to a projectile field `{projectileFieldName}` that does not exist on `{projectileName}`.");
						}
					}
					else if (fieldName == "Warhead")
					{
						if (string.IsNullOrEmpty(field.Value.Value))
						{
							emitWarning($"{field.Location} does not define a warhead type. Skipping unknown field check.");
							continue;
						}

						var warheadName = NormalizeName(field.Value.Value);
						var warheadInfo = modData.ObjectCreator.FindType(warheadName + "Warhead");
						if (warheadInfo == null)
						{
							emitError($"{field.Location} defines unknown warhead `{warheadName}`.");
							continue;
						}

						foreach (var warheadField in field.Value.Nodes)
						{
							var warheadFieldName = NormalizeName(warheadField.Key);
							if (warheadInfo.GetField(warheadFieldName) == null)
								emitError($"{warheadField.Location} refers to a warhead field `{warheadFieldName}` that does not exist on `{warheadName}`.");
						}
					}
					else if (fieldName != "Inherits" && weaponInfo.GetField(fieldName) == null)
						emitError($"{field.Location} refers to a weapon field `{fieldName}` that does not exist.");
				}
			}
		}

		void CheckMapYaml(Action<string> emitError, Action<string> emitWarning, ModData modData, IReadOnlyFileSystem fileSystem, MiniYaml weaponDefinitions)
		{
			if (weaponDefinitions == null)
				return;

			var mapFiles = FieldLoader.GetValue<string[]>("value", weaponDefinitions.Value);
			foreach (var f in mapFiles)
				CheckWeapons(MiniYaml.FromStream(fileSystem.Open(f), f), emitError, emitWarning, modData);

			if (weaponDefinitions.Nodes.Count > 0)
				CheckWeapons(weaponDefinitions.Nodes, emitError, emitWarning, modData);
		}
	}
}
