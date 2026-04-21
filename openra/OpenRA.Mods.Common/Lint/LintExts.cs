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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public static class LintExts
	{
		public static IEnumerable<string> GetFieldValues(object ruleInfo, FieldInfo fieldInfo,
			LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			var type = fieldInfo.FieldType;
			if (type == typeof(string))
				return [(string)fieldInfo.GetValue(ruleInfo)];

			if (typeof(IEnumerable<string>).IsAssignableFrom(type))
				return fieldInfo.GetValue(ruleInfo) as IEnumerable<string> ?? [];

			if (type == typeof(BooleanExpression) || type == typeof(IntegerExpression))
			{
				var expr = (VariableExpression)fieldInfo.GetValue(ruleInfo);
				return expr != null ? expr.Variables : [];
			}

			Type dictionaryInterface = null;
			if (type.IsGenericType)
			{
				if (type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
					dictionaryInterface = type;
				else
					dictionaryInterface = type.GetInterface(typeof(IReadOnlyDictionary<,>).FullName);
			}

			if (dictionaryInterface != null)
			{
				// Use an intermediate list to cover the unlikely case where both keys and values are lintable.
				var dictionaryValues = new List<string>();
				if (dictionaryReference.HasFlag(LintDictionaryReference.Keys) && dictionaryInterface.GenericTypeArguments[0] == typeof(string))
					dictionaryValues.AddRange((IEnumerable<string>)((IDictionary)fieldInfo.GetValue(ruleInfo)).Keys);

				if (dictionaryReference.HasFlag(LintDictionaryReference.Values) && dictionaryInterface.GenericTypeArguments[1] == typeof(string))
					dictionaryValues.AddRange((IEnumerable<string>)((IDictionary)fieldInfo.GetValue(ruleInfo)).Values);

				if (dictionaryReference.HasFlag(LintDictionaryReference.Values) && dictionaryInterface.GenericTypeArguments[1] == typeof(IEnumerable<string>))
					foreach (var row in (IEnumerable<IEnumerable<string>>)((IDictionary)fieldInfo.GetValue(ruleInfo)).Values)
						dictionaryValues.AddRange(row);

				return dictionaryValues;
			}

			var supportedTypes = new[]
			{
				"string", "IEnumerable<string>",
				"IReadOnlyDictionary<string, T> (LintDictionaryReference.Keys)",
				"IReadOnlyDictionary<T, string> (LintDictionaryReference.Values)",
				"IReadOnlyDictionary<T, IEnumerable<string>> (LintDictionaryReference.Values)",
				"BooleanExpression", "IntegerExpression"
			};

			throw new InvalidOperationException(
				$"Bad type for reference on `{ruleInfo.GetType().Name}.{fieldInfo.Name}`. " +
				$"Supported types: {supportedTypes.JoinWith(", ")}.");
		}

		public static IEnumerable<string> GetPropertyValues(object ruleInfo, PropertyInfo propertyInfo,
			LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			var type = propertyInfo.PropertyType;
			if (type == typeof(string))
				return [(string)propertyInfo.GetValue(ruleInfo)];

			if (typeof(IEnumerable).IsAssignableFrom(type))
				return (IEnumerable<string>)propertyInfo.GetValue(ruleInfo);

			if (type == typeof(BooleanExpression) || type == typeof(IntegerExpression))
			{
				var expr = (VariableExpression)propertyInfo.GetValue(ruleInfo);
				return expr != null ? expr.Variables : [];
			}

			Type dictionaryInterface = null;
			if (type.IsGenericType)
			{
				if (type.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
					dictionaryInterface = type;
				else
					dictionaryInterface = type.GetInterface(typeof(IReadOnlyDictionary<,>).FullName);
			}

			if (dictionaryInterface != null)
			{
				// Use an intermediate list to cover the unlikely case where both keys and values are lintable.
				var dictionaryValues = new List<string>();
				if (dictionaryReference.HasFlag(LintDictionaryReference.Keys) && dictionaryInterface.GenericTypeArguments[0] == typeof(string))
					dictionaryValues.AddRange((IEnumerable<string>)((IDictionary)propertyInfo.GetValue(ruleInfo)).Keys);

				if (dictionaryReference.HasFlag(LintDictionaryReference.Values) && dictionaryInterface.GenericTypeArguments[1] == typeof(string))
					dictionaryValues.AddRange((IEnumerable<string>)((IDictionary)propertyInfo.GetValue(ruleInfo)).Values);

				if (dictionaryReference.HasFlag(LintDictionaryReference.Values) && dictionaryInterface.GenericTypeArguments[1] == typeof(IEnumerable<string>))
					foreach (var row in (IEnumerable<IEnumerable<string>>)((IDictionary)propertyInfo.GetValue(ruleInfo)).Values)
						dictionaryValues.AddRange(row);

				return dictionaryValues;
			}

			var supportedTypes = new[]
			{
				"string", "IEnumerable<string>",
				"IReadOnlyDictionary<string, T> (LintDictionaryReference.Keys)",
				"IReadOnlyDictionary<T, string> (LintDictionaryReference.Values)",
				"IReadOnlyDictionary<T, IEnumerable<string>> (LintDictionaryReference.Values)",
				"BooleanExpression", "IntegerExpression"
			};

			throw new InvalidOperationException(
				$"Bad type for reference on `{ruleInfo.GetType().Name}.{propertyInfo.Name}`." +
				$"Supported types: {supportedTypes.JoinWith(", ")}.");
		}
	}
}
