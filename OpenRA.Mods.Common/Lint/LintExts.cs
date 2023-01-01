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
using System.Linq;
using System.Reflection;
using OpenRA.Support;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Lint
{
	public class LintExts
	{
		public static IEnumerable<string> GetFieldValues(object ruleInfo, FieldInfo fieldInfo,
			LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			var type = fieldInfo.FieldType;
			if (type == typeof(string))
				return new[] { (string)fieldInfo.GetValue(ruleInfo) };

			if (typeof(IEnumerable<string>).IsAssignableFrom(type))
				return fieldInfo.GetValue(ruleInfo) as IEnumerable<string>;

			if (type == typeof(BooleanExpression) || type == typeof(IntegerExpression))
			{
				var expr = (VariableExpression)fieldInfo.GetValue(ruleInfo);
				return expr != null ? expr.Variables : Enumerable.Empty<string>();
			}

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				// Use an intermediate list to cover the unlikely case where both keys and values are lintable
				var dictionaryValues = new List<string>();
				if (dictionaryReference.HasFlag(LintDictionaryReference.Keys) && type.GenericTypeArguments[0] == typeof(string))
					dictionaryValues.AddRange((IEnumerable<string>)((IDictionary)fieldInfo.GetValue(ruleInfo)).Keys);

				if (dictionaryReference.HasFlag(LintDictionaryReference.Values) && type.GenericTypeArguments[1] == typeof(string))
					dictionaryValues.AddRange((IEnumerable<string>)((IDictionary)fieldInfo.GetValue(ruleInfo)).Values);

				if (dictionaryReference.HasFlag(LintDictionaryReference.Values) && type.GenericTypeArguments[1] == typeof(IEnumerable<string>))
					foreach (var row in (IEnumerable<IEnumerable<string>>)((IDictionary)fieldInfo.GetValue(ruleInfo)).Values)
						dictionaryValues.AddRange(row);

				return dictionaryValues;
			}

			var supportedTypes = new[]
			{
				"string", "IEnumerable<string>",
				"Dictionary<string, T> (LintDictionaryReference.Keys)",
				"Dictionary<T, string> (LintDictionaryReference.Values)",
				"Dictionary<T, IEnumerable<string>> (LintDictionaryReference.Values)",
				"BooleanExpression", "IntegerExpression"
			};

			throw new InvalidOperationException($"Bad type for reference on {ruleInfo.GetType().Name}.{fieldInfo.Name}. Supported types: {supportedTypes.JoinWith(", ")}");
		}

		public static IEnumerable<string> GetPropertyValues(object ruleInfo, PropertyInfo propertyInfo,
			LintDictionaryReference dictionaryReference = LintDictionaryReference.None)
		{
			var type = propertyInfo.PropertyType;
			if (type == typeof(string))
				return new[] { (string)propertyInfo.GetValue(ruleInfo) };

			if (typeof(IEnumerable).IsAssignableFrom(type))
				return (IEnumerable<string>)propertyInfo.GetValue(ruleInfo);

			if (type == typeof(BooleanExpression) || type == typeof(IntegerExpression))
			{
				var expr = (VariableExpression)propertyInfo.GetValue(ruleInfo);
				return expr != null ? expr.Variables : Enumerable.Empty<string>();
			}

			if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
			{
				// Use an intermediate list to cover the unlikely case where both keys and values are lintable
				var dictionaryValues = new List<string>();
				if (dictionaryReference.HasFlag(LintDictionaryReference.Keys) && type.GenericTypeArguments[0] == typeof(string))
					dictionaryValues.AddRange((IEnumerable<string>)((IDictionary)propertyInfo.GetValue(ruleInfo)).Keys);

				if (dictionaryReference.HasFlag(LintDictionaryReference.Values) && type.GenericTypeArguments[1] == typeof(string))
					dictionaryValues.AddRange((IEnumerable<string>)((IDictionary)propertyInfo.GetValue(ruleInfo)).Values);

				if (dictionaryReference.HasFlag(LintDictionaryReference.Values) && type.GenericTypeArguments[1] == typeof(IEnumerable<string>))
					foreach (var row in (IEnumerable<IEnumerable<string>>)((IDictionary)propertyInfo.GetValue(ruleInfo)).Values)
						dictionaryValues.AddRange(row);

				return dictionaryValues;
			}

			var supportedTypes = new[]
			{
				"string", "IEnumerable<string>",
				"Dictionary<string, T> (LintDictionaryReference.Keys)",
				"Dictionary<T, string> (LintDictionaryReference.Values)",
				"Dictionary<T, IEnumerable<string>> (LintDictionaryReference.Values)",
				"BooleanExpression", "IntegerExpression"
			};

			throw new InvalidOperationException($"Bad type for reference on {ruleInfo.GetType().Name}.{propertyInfo.Name}. Supported types: {supportedTypes.JoinWith(", ")}");
		}
	}
}
