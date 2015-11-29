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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class LintExts
	{
		public static IEnumerable<string> GetFieldValues(object ruleInfo, FieldInfo fieldInfo, Action<string> emitError)
		{
			var type = fieldInfo.FieldType;
			var value = fieldInfo.GetValue(ruleInfo);

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				var getValues = type.GetProperty("Values").GetGetMethod();
				type = type.GetGenericArguments()[1];
				var errorFree = true;
				Action<string> emitCustomError = message =>
				{
					emitError("Bad type for reference values on {0}.{1}. {2}".F(ruleInfo.GetType().Name, fieldInfo.Name, message));
					errorFree = false;
				};

				foreach (var v in getValues.Invoke(value, null) as IEnumerable)
					if (v != null && errorFree)
						foreach (var s in GetStringsFromValue(v, type, emitCustomError))
							yield return s;
			}
			else
			{
				Action<string> emitCustomError = message =>
					emitError("Bad type for reference on {0}.{1}. {2}".F(ruleInfo.GetType().Name, fieldInfo.Name, message));
				foreach (var s in GetStringsFromValue(value, type, emitCustomError))
					yield return s;
			}
		}

		public static IEnumerable<string> GetStringsFromValue(object value, Type type, Action<string> emitError)
		{
			if (type == typeof(string))
				return new[] { (string)value };
			if (type == typeof(string[]))
				return (string[])value;
			if (type == typeof(HashSet<string>))
				return (HashSet<string>)value;

			emitError("Supported types: string, string[], HashSet<string>");
			return Enumerable.Empty<string>();
		}

		public static IEnumerable<string> GetAllActorTraitValuesHavingAttribute<T>(Action<string> emitError, Ruleset rules) where T : Attribute
		{
			foreach (var actorInfo in rules.Actors)
			{
				foreach (var trait in actorInfo.Value.TraitInfos<ITraitInfo>())
				{
					var fields = trait.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<T>()))
					{
						var values = GetFieldValues(trait, field, emitError);
						foreach (var value in values)
							yield return value;
					}
				}
			}
		}

		public static IEnumerable<string> GetAllWarheadValuesHavingAttribute<T>(Action<string> emitError, Ruleset rules) where T : Attribute
		{
			foreach (var weapon in rules.Weapons)
			{
				foreach (var warhead in weapon.Value.Warheads)
				{
					var fields = warhead.GetType().GetFields();
					foreach (var field in fields.Where(x => x.HasAttribute<UpgradeGrantedReferenceAttribute>()))
					{
						var values = GetFieldValues(warhead, field, emitError);
						foreach (var value in values)
							yield return value;
					}
				}
			}
		}
	}
}
