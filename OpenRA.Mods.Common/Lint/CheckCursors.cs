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
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	class CheckCursors : ILintRulesPass, ILintServerMapPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			Run(emitError, modData, rules);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			Run(emitError, modData, mapRules);
		}

		void Run(Action<string> emitError, ModData modData, Ruleset rules)
		{
			var fileSystem = modData.DefaultFileSystem;
			var sequenceYaml = MiniYaml.Merge(modData.Manifest.Cursors.Select(s => MiniYaml.FromStream(fileSystem.Open(s), s)));
			var nodesDict = new MiniYaml(null, sequenceYaml).ToDictionary();

			// Avoid using CursorProvider as it attempts to load palettes from the file system.
			var cursors = new List<string>();
			foreach (var s in nodesDict["Cursors"].Nodes)
				foreach (var sequence in s.Value.Nodes)
					cursors.Add(sequence.Key);

			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields();
					foreach (var field in fields)
					{
						var cursorReference = field.GetCustomAttributes<CursorReferenceAttribute>(true).FirstOrDefault();
						if (cursorReference == null)
							continue;

						var cursor = LintExts.GetFieldValues(traitInfo, field, cursorReference.DictionaryReference).FirstOrDefault();
						if (string.IsNullOrEmpty(cursor))
							continue;

						if (!cursors.Contains(cursor))
							emitError($"Undefined cursor {cursor} for actor {actorInfo.Value.Name} with trait {traitInfo}.");
					}
				}
			}
		}
	}
}
