#region Copyright & License Information
/*
 * Copyright 2007-2011 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation. For more information,
 * see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Reflection;
using OpenRA.Traits;

namespace OpenRA.Mods.RA
{
	public class CheckActorReferences : ILintPass
	{
		Action<string> EmitError;

		public void Run(Action<string> emitError, Action<string> emitWarning, Map map)
		{
			EmitError = emitError;

			foreach (var actorInfo in Rules.Info)
				foreach (var traitInfo in actorInfo.Value.Traits.WithInterface<ITraitInfo>())
					CheckTrait(actorInfo.Value, traitInfo);
		}

		void CheckTrait(ActorInfo actorInfo, ITraitInfo traitInfo)
		{
			var actualType = traitInfo.GetType();
			foreach (var field in actualType.GetFields())
			{
				if (field.HasAttribute<ActorReferenceAttribute>())
					CheckReference(actorInfo, traitInfo, field, Rules.Info, "actor");
				if (field.HasAttribute<WeaponReferenceAttribute>())
					CheckReference(actorInfo, traitInfo, field, Rules.Weapons, "weapon");
				if (field.HasAttribute<VoiceReferenceAttribute>())
					CheckReference(actorInfo, traitInfo, field, Rules.Voices, "voice");
			}
		}

		string[] GetFieldValues(ITraitInfo traitInfo, FieldInfo fieldInfo)
		{
			var type = fieldInfo.FieldType;
			if (type == typeof(string))
				return new string[] { (string)fieldInfo.GetValue(traitInfo) };
			if (type == typeof(string[]))
				return (string[])fieldInfo.GetValue(traitInfo);

			EmitError("Bad type for reference on {0}.{1}. Supported types: string, string[]"
				.F(traitInfo.GetType().Name, fieldInfo.Name));

			return new string[] { };
		}

		void CheckReference<T>(ActorInfo actorInfo, ITraitInfo traitInfo, FieldInfo fieldInfo,
			Dictionary<string, T> dict, string type)
		{
			var values = GetFieldValues(traitInfo, fieldInfo);
			foreach (var v in values)
				if (v != null && !dict.ContainsKey(v.ToLowerInvariant()))
					EmitError("{0}.{1}.{2}: Missing {3} `{4}`."
						.F(actorInfo.Name, traitInfo.GetType().Name, fieldInfo.Name, type, v));
		}
	}
}

