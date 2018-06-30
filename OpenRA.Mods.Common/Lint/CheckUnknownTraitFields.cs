#region Copyright & License Information
/*
 * Copyright 2007-2018 The OpenRA Developers (see AUTHORS)
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
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckUnknownTraitFields : ILintPass, ILintMapPass
	{
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
					// Removals can never define children
					if (t.Key.StartsWith("-", StringComparison.Ordinal) && t.Value.Nodes.Any())
					{
						emitError("{0} has child nodes, which is not valid for removals.".F(t.Key));
						continue;
					}

					var traitName = NormalizeName(t.Key);
					var traitInfo = modData.ObjectCreator.FindType(traitName + "Info");
					foreach (var field in t.Value.Nodes)
					{
						var fieldName = NormalizeName(field.Key);
						if (traitInfo.GetField(fieldName) == null)
							emitError("{0} refers to a trait field `{1}` that does not exist on `{2}`.".F(field.Location, fieldName, traitName));
					}
				}
			}
		}

		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			foreach (var f in modData.Manifest.Rules)
				CheckActors(MiniYaml.FromStream(modData.DefaultFileSystem.Open(f), f), emitError, modData);
		}

		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			if (map.RuleDefinitions != null && map.RuleDefinitions.Value != null)
			{
				var mapFiles = FieldLoader.GetValue<string[]>("value", map.RuleDefinitions.Value);
				foreach (var f in mapFiles)
					CheckActors(MiniYaml.FromStream(map.Open(f), f), emitError, modData);

				if (map.RuleDefinitions.Nodes.Any())
					CheckActors(map.RuleDefinitions.Nodes, emitError, modData);
			}
		}
	}
}
