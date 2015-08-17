#region Copyright & License Information
/*
 * Copyright 2007-2015 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckPalettes : ILintPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			if (map != null && !map.RuleDefinitions.Any())
				return;

			var rules = map == null ? Game.ModData.DefaultRules : map.Rules;

			var palettes = GetPalettes(emitError, rules).ToList();

			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.Traits)
				{
					var fields = traitInfo.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<PaletteReferenceAttribute>()))
					{
						var references = LintExts.GetFieldValues(traitInfo, field, emitError);
						foreach (var reference in references)
						{
							if (string.IsNullOrEmpty(reference))
								continue;

							if (!palettes.Contains(reference))
								emitError("Undefined palette reference {0} detected at {1}".F(reference, traitInfo));
						}
					}
				}
			}

			foreach (var weaponInfo in rules.Weapons)
			{
				var projectileInfo = weaponInfo.Value.Projectile;
				if (projectileInfo == null)
					continue;

				foreach (var field in projectileInfo.GetType().GetFields())
				{
					if (field.HasAttribute<PaletteReferenceAttribute>())
					{
						var references = LintExts.GetFieldValues(projectileInfo, field, emitError);
						foreach (var reference in references)
						{
							if (string.IsNullOrEmpty(reference))
								continue;

							if (!palettes.Contains(reference))
								emitError("Undefined palette reference {0} detected at weapon {1}.".F(reference, weaponInfo.Key));
						}
					}
				}
			}
		}

		static IEnumerable<string> GetPalettes(Action<string> emitError, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.Traits)
				{
					var fields = traitInfo.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<PaletteDefinitionAttribute>()))
					{
						var values = LintExts.GetFieldValues(traitInfo, field, emitError);
						foreach (var value in values)
							yield return value;
					}
				}
			}
		}
	}
}
