#region Copyright & License Information
/*
 * Copyright 2007-2016 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckPalettes : ILintRulesPass
	{
		List<string> palettes = new List<string>();
		List<string> playerPalettes = new List<string>();

		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			GetPalettes(emitError, rules);

			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<ITraitInfo>())
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

						var references = LintExts.GetFieldValues(traitInfo, field, emitError);
						foreach (var reference in references)
						{
							if (string.IsNullOrEmpty(reference))
								continue;

							if (isPlayerPalette)
							{
								if (!playerPalettes.Contains(reference))
									emitError("Undefined player palette reference {0} detected at {1} for {2}".F(reference, traitInfo, actorInfo.Key));
							}
							else
							{
								if (!palettes.Contains(reference))
									emitError("Undefined palette reference {0} detected at {1} for {2}".F(reference, traitInfo, actorInfo.Key));
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

					var references = LintExts.GetFieldValues(projectileInfo, field, emitError);
					foreach (var reference in references)
					{
						if (string.IsNullOrEmpty(reference))
							continue;

						if (isPlayerPalette)
						{
							if (!playerPalettes.Contains(reference))
								emitError("Undefined player palette reference {0} detected at weapon {1}.".F(reference, weaponInfo.Key));
						}
						else
						{
							if (!palettes.Contains(reference))
								emitError("Undefined palette reference {0} detected at weapon {1}.".F(reference, weaponInfo.Key));
						}
					}
				}
			}
		}

		void GetPalettes(Action<string> emitError, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<ITraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<PaletteDefinitionAttribute>()))
					{
						var paletteDefinition = field.GetCustomAttributes<PaletteDefinitionAttribute>(true).First();
						var values = LintExts.GetFieldValues(traitInfo, field, emitError);
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
