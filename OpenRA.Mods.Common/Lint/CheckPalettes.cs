#region Copyright & License Information
/*
 * Copyright 2007-2022 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckPalettes : ILintRulesPass, ILintServerMapPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			Run(emitError, rules);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			Run(emitError, mapRules);
		}

		void Run(Action<string> emitError, Ruleset rules)
		{
			var palettes = new List<string>();
			var playerPalettes = new List<string>();
			GetPalettes(rules, palettes, playerPalettes);

			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<PaletteReferenceAttribute>()))
					{
						var isPlayerPalette = false;

						var paletteReference = field.GetCustomAttributes<PaletteReferenceAttribute>(true).FirstOrDefault();
						if (paletteReference != null)
						{
							if (!string.IsNullOrEmpty(paletteReference.PlayerPaletteReferenceSwitch))
							{
								var fieldInfo = fields.First(f => f.Name == paletteReference.PlayerPaletteReferenceSwitch);
								isPlayerPalette = (bool)fieldInfo.GetValue(traitInfo);
							}
							else
								isPlayerPalette = paletteReference.IsPlayerPalette;
						}

						var references = LintExts.GetFieldValues(traitInfo, field);
						foreach (var reference in references)
						{
							if (string.IsNullOrEmpty(reference))
								continue;

							if (isPlayerPalette)
							{
								if (!playerPalettes.Contains(reference))
									emitError($"Undefined player palette reference {reference} detected at {traitInfo} for {actorInfo.Key}");
							}
							else
							{
								if (!palettes.Contains(reference))
									emitError($"Undefined palette reference {reference} detected at {traitInfo} for {actorInfo.Key}");
							}
						}
					}
				}
			}

			foreach (var weaponInfo in rules.Weapons)
			{
				var projectileInfo = weaponInfo.Value.Projectile;
				if (projectileInfo == null)
					continue;

				var fields = projectileInfo.GetType().GetFields();
				foreach (var field in fields.Where(x => x.HasAttribute<PaletteReferenceAttribute>()))
				{
					var isPlayerPalette = false;

					var paletteReference = field.GetCustomAttributes<PaletteReferenceAttribute>(true).First();
					if (paletteReference != null)
					{
						if (!string.IsNullOrEmpty(paletteReference.PlayerPaletteReferenceSwitch))
						{
							var fieldInfo = fields.First(f => f.Name == paletteReference.PlayerPaletteReferenceSwitch);
							isPlayerPalette = (bool)fieldInfo.GetValue(projectileInfo);
						}
						else
							isPlayerPalette = paletteReference.IsPlayerPalette;
					}

					var references = LintExts.GetFieldValues(projectileInfo, field);
					foreach (var reference in references)
					{
						if (string.IsNullOrEmpty(reference))
							continue;

						if (isPlayerPalette)
						{
							if (!playerPalettes.Contains(reference))
								emitError($"Undefined player palette reference {reference} detected at weapon {weaponInfo.Key}.");
						}
						else
						{
							if (!palettes.Contains(reference))
								emitError($"Undefined palette reference {reference} detected at weapon {weaponInfo.Key}.");
						}
					}
				}
			}
		}

		void GetPalettes(Ruleset rules, List<string> palettes, List<string> playerPalettes)
		{
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<PaletteDefinitionAttribute>()))
					{
						var paletteDefinition = field.GetCustomAttributes<PaletteDefinitionAttribute>(true).First();
						var values = LintExts.GetFieldValues(traitInfo, field);
						foreach (var value in values)
						{
							if (paletteDefinition.IsPlayerPalette)
								playerPalettes.Add(value);
							else
								palettes.Add(value);
						}
					}
				}
			}
		}
	}
}
