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
using System.Linq;
using OpenRA.Mods.Common.Traits;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckVoiceReferences : ILintRulesPass, ILintServerMapPass
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
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var traitInfo in actorInfo.Value.TraitInfos<TraitInfo>())
				{
					var fields = traitInfo.GetType().GetFields().Where(f => f.HasAttribute<VoiceSetReferenceAttribute>());
					foreach (var field in fields)
					{
						var voiceSets = LintExts.GetFieldValues(traitInfo, field);
						foreach (var voiceSet in voiceSets)
						{
							if (string.IsNullOrEmpty(voiceSet))
								continue;

							CheckVoices(actorInfo.Value, emitError, rules, voiceSet);
						}
					}
				}
			}
		}

		void CheckVoices(ActorInfo actorInfo, Action<string> emitError, Ruleset rules, string voiceSet)
		{
			var soundInfo = rules.Voices[voiceSet.ToLowerInvariant()];

			foreach (var traitInfo in actorInfo.TraitInfos<TraitInfo>())
			{
				var fields = traitInfo.GetType().GetFields().Where(f => f.HasAttribute<VoiceReferenceAttribute>());
				foreach (var field in fields)
				{
					var voices = LintExts.GetFieldValues(traitInfo, field);
					foreach (var voice in voices)
					{
						if (string.IsNullOrEmpty(voice))
							continue;

						if (!soundInfo.Voices.Keys.Contains(voice))
							emitError($"Actor {actorInfo.Name} using voice set {voiceSet} does not define {voice} voice required by {traitInfo}.");
					}
				}
			}
		}
	}
}
