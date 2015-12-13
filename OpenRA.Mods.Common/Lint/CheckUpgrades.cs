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
	public class CheckUpgrades : ILintRulesPass
	{
		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			CheckUpgradesValidity(emitError, rules);
			CheckUpgradesUsage(emitError, emitWarning, rules);
		}

		static void CheckUpgradesValidity(Action<string> emitError, Ruleset rules)
		{
			var upgradesGranted = LintExts.GetAllActorTraitValuesHavingAttribute<UpgradeGrantedReferenceAttribute>(emitError, rules)
				.Concat(LintExts.GetAllWarheadValuesHavingAttribute<UpgradeGrantedReferenceAttribute>(emitError, rules))
				.ToHashSet();

			foreach (var actorInfo in rules.Actors)
			{
				foreach (var trait in actorInfo.Value.TraitInfos<ITraitInfo>())
				{
					var fields = trait.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeUsedReferenceAttribute>()))
					{
						var values = LintExts.GetFieldValues(trait, field, emitError);
						foreach (var value in values.Where(x => !upgradesGranted.Contains(x)))
							emitError("Actor type `{0}` uses upgrade `{1}` that is not granted by anything!".F(actorInfo.Key, value));
					}
				}
			}
		}

		static void CheckUpgradesUsage(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			var upgradesUsed = LintExts.GetAllActorTraitValuesHavingAttribute<UpgradeUsedReferenceAttribute>(emitError, rules).ToHashSet();

			// Check all upgrades granted by traits.
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var trait in actorInfo.Value.TraitInfos<ITraitInfo>())
				{
					var fields = trait.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeGrantedReferenceAttribute>()))
					{
						var values = LintExts.GetFieldValues(trait, field, emitError);
						foreach (var value in values.Where(x => !upgradesUsed.Contains(x)))
							emitWarning("Actor type `{0}` grants upgrade `{1}` that is not used by anything!".F(actorInfo.Key, value));
					}
				}
			}

			// Check all upgrades granted by warheads.
			foreach (var weapon in rules.Weapons)
			{
				foreach (var warhead in weapon.Value.Warheads)
				{
					var fields = warhead.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeGrantedReferenceAttribute>()))
					{
						var values = LintExts.GetFieldValues(warhead, field, emitError);
						foreach (var value in values.Where(x => !upgradesUsed.Contains(x)))
							emitWarning("Weapon type `{0}` grants upgrade `{1}` that is not used by anything!".F(weapon.Key, value));
					}
				}
			}
		}
	}
}