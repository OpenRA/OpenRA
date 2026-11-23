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
using System.Reflection;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Traits;
using OpenRA.Server;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckActorReferences : ILintRulesPass, ILintServerMapPass
	{
		void ILintRulesPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, Ruleset rules)
		{
			foreach (var actorInfo in rules.Actors.Values)
				foreach (var traitInfo in actorInfo.TraitInfos<TraitInfo>())
					CheckTrait(emitError, actorInfo, traitInfo, rules);
		}

		void ILintServerMapPass.Run(Action<string> emitError, Action<string> emitWarning, ModData modData, MapPreview map, Ruleset mapRules)
		{
			foreach (var actorInfo in mapRules.Actors.Values)
				foreach (var traitInfo in actorInfo.TraitInfos<TraitInfo>())
					CheckTrait(emitError, actorInfo, traitInfo, mapRules);
		}

		static void CheckTrait(Action<string> emitError, ActorInfo actorInfo, TraitInfo traitInfo, Ruleset rules)
		{
			var actualType = traitInfo.GetType();
			foreach (var field in Utility.GetFields(actualType))
			{
				if (Utility.HasAttribute<ActorReferenceAttribute>(field))
					CheckActorReference(emitError, actorInfo, traitInfo, field, rules.Actors,
						Utility.GetCustomAttributes<ActorReferenceAttribute>(field, true)[0]);

				if (Utility.HasAttribute<WeaponReferenceAttribute>(field))
					CheckWeaponReference(emitError, actorInfo, traitInfo, field, rules.Weapons);

				if (Utility.HasAttribute<VoiceSetReferenceAttribute>(field))
					CheckVoiceReference(emitError, actorInfo, traitInfo, field, rules.Voices);
			}
		}

		static void CheckActorReference(Action<string> emitError, ActorInfo actorInfo, TraitInfo traitInfo,
			FieldInfo fieldInfo, ActorInfoDictionary dict, ActorReferenceAttribute attribute)
		{
			var values = LintExts.GetFieldValues(traitInfo, fieldInfo, attribute.DictionaryReference);
			if (values == null)
				return;

			foreach (var value in values)
			{
				if (value == null)
					continue;

				if (!dict.TryGetValue(value, out var actor))
				{
					emitError($"`{actorInfo.Name}.{traitInfo.GetType().Name}.{fieldInfo.Name}`: Missing actor `{value}`.");

					continue;
				}

				foreach (var requiredTrait in attribute.RequiredTraits)
					if (!actor.TraitsInConstructOrder().Any(t => t.GetType() == requiredTrait || t.GetType().IsSubclassOf(requiredTrait)))
						emitError($"Actor type `{value}` does not have trait `{requiredTrait.Name}` which is required by `{traitInfo.GetType().Name}.{fieldInfo.Name}`.");
			}
		}

		static void CheckWeaponReference(Action<string> emitError, ActorInfo actorInfo, TraitInfo traitInfo,
			FieldInfo fieldInfo, IReadOnlyDictionary<string, WeaponInfo> dict)
		{
			var values = LintExts.GetFieldValues(traitInfo, fieldInfo);
			foreach (var value in values)
			{
				if (value == null)
					continue;

				if (!dict.ContainsKey(value.ToLowerInvariant()))
					emitError($"`{actorInfo.Name}.{traitInfo.GetType().Name}.{fieldInfo.Name}`: Missing weapon `{value}`.");
			}
		}

		static void CheckVoiceReference(Action<string> emitError, ActorInfo actorInfo, TraitInfo traitInfo,
			FieldInfo fieldInfo, IReadOnlyDictionary<string, SoundInfo> dict)
		{
			var values = LintExts.GetFieldValues(traitInfo, fieldInfo);
			foreach (var value in values)
			{
				if (value == null)
					continue;

				if (!dict.ContainsKey(value.ToLowerInvariant()))
					emitError($"`{actorInfo.Name}.{traitInfo.GetType().Name}.{fieldInfo.Name}`: Missing voice `{value}`.");
			}
		}
	}
}
