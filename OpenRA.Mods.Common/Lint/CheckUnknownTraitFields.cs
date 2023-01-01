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
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	class CheckUnknownTraitFields : ILintPass, ILintMapPass, ILintServerMapPass
	{
		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			foreach (var f in modData.Manifest.Rules)
				CheckActors(MiniYaml.FromStream(modData.DefaultFileSystem.Open(f), f), emitError, modData);
		}

		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			CheckMapYaml(emitError, modData, map, map.RuleDefinitions);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			CheckMapYaml(emitError, modData, map, map.RuleDefinitions);
		}

		string NormalizeName(string key)
		{
			var name = key.Split('@')[0];
			if (name.StartsWith("-", StringComparison.Ordinal))
				return name.Substring(1);

			return name;
		}

		void CheckActors(IEnumerable<MiniYamlNode> actors, Action<string> emitError, ModData modData)
		{
			foreach (var actor in actors)
			{
				foreach (var t in actor.Value.Nodes)
				{
					// Removals can never define children or values
					if (t.Key.StartsWith("-", StringComparison.Ordinal))
					{
						if (t.Value.Nodes.Count > 0)
							emitError($"{t.Location} {t.Key} defines child nodes, which are not valid for removals.");

						if (!string.IsNullOrEmpty(t.Value.Value))
							emitError($"{t.Location} {t.Key} defines a value, which is not valid for removals.");

						continue;
					}

					var traitName = NormalizeName(t.Key);

					// Inherits can never define children
					if (traitName == "Inherits")
					{
						if (t.Value.Nodes.Count > 0)
							emitError($"{t.Location} defines child nodes, which are not valid for Inherits.");

						continue;
					}

					var traitInfo = modData.ObjectCreator.FindType(traitName + "Info");
					if (traitInfo == null)
					{
						emitError($"{t.Location} defines unknown trait `{traitName}`.");
						continue;
					}

					foreach (var field in t.Value.Nodes)
					{
						var fieldName = NormalizeName(field.Key);
						if (traitInfo.GetField(fieldName) == null)
							emitError($"{field.Location} refers to a trait field `{fieldName}` that does not exist on `{traitName}`.");
					}
				}
			}
		}

		void CheckMapYaml(Action<string> emitError, ModData modData, IReadOnlyFileSystem fileSystem, MiniYaml ruleDefinitions)
		{
			if (ruleDefinitions == null)
				return;

			var mapFiles = FieldLoader.GetValue<string[]>("value", ruleDefinitions.Value);
			foreach (var f in mapFiles)
				CheckActors(MiniYaml.FromStream(fileSystem.Open(f), f), emitError, modData);

			if (ruleDefinitions.Nodes.Count > 0)
				CheckActors(ruleDefinitions.Nodes, emitError, modData);
		}
	}
}
