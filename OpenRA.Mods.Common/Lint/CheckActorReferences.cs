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
using System.Linq;
using System.Reflection;
using OpenRA.GameRules;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class CheckActorReferences : ILintRulesPass
	{
		Action<string> emitError;

		public void Run(Action<string> emitError, Action<string> emitWarning, Ruleset rules)
		{
			this.emitError = emitError;

			foreach (var actorInfo in rules.Actors)
				foreach (var traitInfo in actorInfo.Value.TraitInfos<ITraitInfo>())
					CheckTrait(actorInfo.Value, traitInfo, rules);
		}

		void CheckTrait(ActorInfo actorInfo, ITraitInfo traitInfo, Ruleset rules)
		{
			var actualType = traitInfo.GetType();
			foreach (var field in actualType.GetFields())
			{
				if (field.HasAttribute<ActorReferenceAttribute>())
					CheckActorReference(actorInfo, traitInfo, field, rules.Actors,
						field.GetCustomAttributes<ActorReferenceAttribute>(true)[0]);

				if (field.HasAttribute<WeaponReferenceAttribute>())
					CheckWeaponReference(actorInfo, traitInfo, field, rules.Weapons,
						field.GetCustomAttributes<WeaponReferenceAttribute>(true)[0]);

				if (field.HasAttribute<VoiceSetReferenceAttribute>())
					CheckVoiceReference(actorInfo, traitInfo, field, rules.Voices,
						field.GetCustomAttributes<VoiceSetReferenceAttribute>(true)[0]);
			}
		}

		void CheckActorReference(ActorInfo actorInfo,
			ITraitInfo traitInfo,
			FieldInfo fieldInfo,
			IReadOnlyDictionary<string, ActorInfo> dict,
			ActorReferenceAttribute attribute)
		{
			var values = LintExts.GetFieldValues(traitInfo, fieldInfo, emitError);
			foreach (var value in values)
			{
				if (value == null)
					continue;

				// NOTE: Once https://github.com/OpenRA/OpenRA/issues/4124 is resolved we won't
				//       have to .ToLower* anything here.
				var v = value.ToLowerInvariant();

				if (!dict.ContainsKey(v))
				{
					emitError("{0}.{1}.{2}: Missing actor `{3}`."
						.F(actorInfo.Name, traitInfo.GetType().Name, fieldInfo.Name, value));

					continue;
				}

				foreach (var requiredTrait in attribute.RequiredTraits)
					if (!dict[v].TraitsInConstructOrder().Any(t => t.GetType() == requiredTrait || t.GetType().IsSubclassOf(requiredTrait)))
						emitError("Actor type {0} does not have trait {1} which is required by {2}.{3}."
							.F(value, requiredTrait.Name, traitInfo.GetType().Name, fieldInfo.Name));
			}
		}

		void CheckWeaponReference(ActorInfo actorInfo,
			ITraitInfo traitInfo,
			FieldInfo fieldInfo,
			IReadOnlyDictionary<string, WeaponInfo> dict,
			WeaponReferenceAttribute attribute)
		{
			var values = LintExts.GetFieldValues(traitInfo, fieldInfo, emitError);
			foreach (var value in values)
			{
				if (value == null)
					continue;

				if (!dict.ContainsKey(value.ToLower()))
					emitError("{0}.{1}.{2}: Missing weapon `{3}`."
						.F(actorInfo.Name, traitInfo.GetType().Name, fieldInfo.Name, value));
			}
		}

		void CheckVoiceReference(ActorInfo actorInfo,
			ITraitInfo traitInfo,
			FieldInfo fieldInfo,
			IReadOnlyDictionary<string, SoundInfo> dict,
			VoiceSetReferenceAttribute attribute)
		{
			var values = LintExts.GetFieldValues(traitInfo, fieldInfo, emitError);
			foreach (var value in values)
			{
				if (value == null)
					continue;

				if (!dict.ContainsKey(value.ToLower()))
					emitError("{0}.{1}.{2}: Missing voice `{3}`."
						.F(actorInfo.Name, traitInfo.GetType().Name, fieldInfo.Name, value));
			}
		}
	}
}
