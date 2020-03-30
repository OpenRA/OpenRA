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

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.GameRules;

namespace OpenRA.Mods.Common.Lint
{
	class CheckUnknownWeaponFields : ILintPass, ILintMapPass
	{
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
						if (field.Value.Nodes.Any())
							emitError("{0} {1} defines child nodes, which is not valid for removals.".F(field.Location, field.Key));

						if (!string.IsNullOrEmpty(field.Value.Value))
							emitError("{0} {1} defines a value, which is not valid for removals.".F(field.Location, field.Key));

						continue;
					}

					var fieldName = NormalizeName(field.Key);
					if (fieldName == "Projectile" && !string.IsNullOrEmpty(field.Value.Value))
					{
						var projectileName = NormalizeName(field.Value.Value);
						var projectileInfo = modData.ObjectCreator.FindType(projectileName + "Info");
						foreach (var projectileField in field.Value.Nodes)
						{
							var projectileFieldName = NormalizeName(projectileField.Key);
							if (projectileInfo.GetField(projectileFieldName) == null)
								emitError("{0} refers to a projectile field `{1}` that does not exist on `{2}`.".F(projectileField.Location, projectileFieldName, projectileName));
						}
					}
					else if (fieldName == "Warhead")
					{
						if (string.IsNullOrEmpty(field.Value.Value))
						{
							emitWarning("{0} does not define a warhead type. Skipping unknown field check.".F(field.Location));
							continue;
						}

						var warheadName = NormalizeName(field.Value.Value);
						var warheadInfo = modData.ObjectCreator.FindType(warheadName + "Warhead");
						foreach (var warheadField in field.Value.Nodes)
						{
							var warheadFieldName = NormalizeName(warheadField.Key);
							if (warheadInfo.GetField(warheadFieldName) == null)
								emitError("{0} refers to a warhead field `{1}` that does not exist on `{2}`.".F(warheadField.Location, warheadFieldName, warheadName));
						}
					}
					else if (fieldName != "Inherits" && weaponInfo.GetField(fieldName) == null)
						emitError("{0} refers to a weapon field `{1}` that does not exist.".F(field.Location, fieldName));
				}
			}
		}

		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			foreach (var f in modData.Manifest.Weapons)
				CheckWeapons(MiniYaml.FromStream(modData.DefaultFileSystem.Open(f), f), emitError, emitWarning, modData);
		}

		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			if (map.WeaponDefinitions != null && map.WeaponDefinitions.Value != null)
			{
				var mapFiles = FieldLoader.GetValue<string[]>("value", map.WeaponDefinitions.Value);
				foreach (var f in mapFiles)
					CheckWeapons(MiniYaml.FromStream(map.Open(f), f), emitError, emitWarning, modData);

				if (map.WeaponDefinitions.Nodes.Any())
					CheckWeapons(map.WeaponDefinitions.Nodes, emitError, emitWarning, modData);
			}
		}
	}
}
