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
using System.Linq;
using OpenRA.FileSystem;
using OpenRA.Mods.Common.UpdateRules;
using OpenRA.Server;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckWorldAndPlayerInherits : ILintPass, ILintMapPass, ILintServerMapPass
	{
		void ILintPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData)
		{
			var nodes = new List<MiniYamlNode>();
			foreach (var f in modData.Manifest.Rules)
				nodes.AddRange(MiniYaml.FromStream(modData.DefaultFileSystem.Open(f), f));

			Run(emitError, nodes);
		}

		void ILintMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Map map)
		{
			CheckMapYaml(emitError, modData, map, map.RuleDefinitions);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			CheckMapYaml(emitError, modData, map, map.RuleDefinitions);
		}

		void CheckMapYaml(Action<string> emitError, ModData modData, IReadOnlyFileSystem fileSystem, MiniYaml ruleDefinitions)
		{
			if (ruleDefinitions == null)
				return;

			var files = modData.Manifest.Rules.AsEnumerable();
			if (ruleDefinitions.Value != null)
			{
				var mapFiles = FieldLoader.GetValue<string[]>("value", ruleDefinitions.Value);
				files = files.Append(mapFiles);
			}

			var nodes = new List<MiniYamlNode>();
			foreach (var f in files)
				nodes.AddRange(MiniYaml.FromStream(fileSystem.Open(f), f));

			nodes.AddRange(ruleDefinitions.Nodes);
			Run(emitError, nodes);
		}

		void Run(Action<string> emitError, List<MiniYamlNode> nodes)
		{
			// Build a list of all inheritance relationships
			var inheritsMap = new Dictionary<string, List<string>>();
			foreach (var actorNode in nodes)
			{
				var inherits = inheritsMap.GetOrAdd(actorNode.Key, _ => new List<string>());
				foreach (var inheritsNode in actorNode.ChildrenMatching("Inherits"))
					inherits.Add(inheritsNode.Value.Value);
			}

			CheckInheritance(emitError, "World", inheritsMap);
			CheckInheritance(emitError, "Player", inheritsMap);
		}

		void CheckInheritance(Action<string> emitError, string actor, Dictionary<string, List<string>> inheritsMap)
		{
			var toResolve = new Queue<string>(inheritsMap.Keys.Where(k => string.Equals(k, actor, StringComparison.InvariantCultureIgnoreCase)));
			while (toResolve.TryDequeue(out var key))
			{
				// Missing keys are a fatal merge error, so will have already been reported by other lint checks
				if (!inheritsMap.TryGetValue(key, out var inherits))
					continue;

				foreach (var inherit in inherits)
				{
					if (inherit[0] != '^')
						emitError($"{actor} definition inherits from {inherit}, which is not an abstract template.");

					toResolve.Enqueue(inherit);
				}
			}
		}
	}
}
